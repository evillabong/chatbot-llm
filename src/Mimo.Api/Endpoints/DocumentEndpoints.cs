using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Document;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Pgvector;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Endpoints CRUD de documentos de conocimiento.
/// La lectura está disponible para cualquier funcionario (política Agent);
/// la administración (crear, actualizar, desactivar, reindexar) requiere el
/// rol Administrador (política TenantAdmin).
/// Al crear o actualizar con contenido nuevo se regenera el embedding automáticamente.
/// </summary>
public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents")
            .WithTags("Documents")
            .RequireAuthorization(MimoAuthorization.Policies.Agent);

        group.MapGet("/", ListDocumentsAsync)
            .WithName("ListDocuments")
            .WithSummary("Lista documentos del tenant con filtros opcionales.");

        group.MapGet("/{id:guid}", GetDocumentAsync)
            .WithName("GetDocument")
            .WithSummary("Obtiene un documento por su ID.");

        group.MapPost("/", CreateDocumentAsync)
            .WithName("CreateDocument")
            .WithSummary("Crea un documento y genera su embedding para búsqueda semántica.")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapPut("/{id:guid}", UpdateDocumentAsync)
            .WithName("UpdateDocument")
            .WithSummary("Actualiza un documento. Regenera embedding si cambia el contenido.")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapDelete("/{id:guid}", DeactivateDocumentAsync)
            .WithName("DeactivateDocument")
            .WithSummary("Desactiva un documento (borrado lógico).")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        // Endpoint para regenerar el embedding manualmente
        group.MapPost("/{id:guid}/reindex", ReindexDocumentAsync)
            .WithName("ReindexDocument")
            .WithSummary("Regenera el embedding de un documento existente.")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> ListDocumentsAsync(
        IDocumentRepository repo,
        bool? isActive = null,
        Guid? categoryId = null,
        Guid? roleId = null,
        CancellationToken ct = default)
    {
        var docs = await repo.ListAsync(isActive, categoryId, roleId, ct);
        return Results.Ok(docs.Select(ToResponse).ToList());
    }

    private static async Task<IResult> GetDocumentAsync(
        Guid id, IDocumentRepository repo, CancellationToken ct = default)
    {
        var doc = await repo.GetByIdAsync(id, ct);
        return doc is null
            ? Results.NotFound(new { error = "Documento no encontrado." })
            : Results.Ok(ToResponse(doc));
    }

    private static async Task<IResult> CreateDocumentAsync(
        CreateDocumentRequest request,
        IDocumentRepository repo,
        IVectorSearchService vectorSearch,
        HttpContext context,
        CancellationToken ct = default)
    {
        var tenantId = (Guid)context.Items["TenantId"]!;

        // Generar embedding del contenido para búsqueda semántica
        Vector? embedding = null;
        try
        {
            var raw = await vectorSearch.GetEmbeddingAsync(
                tenantId, $"{request.Title}\n{request.Content}", ct);
            embedding = new Vector(raw);
        }
        catch (Exception ex)
        {
            // El documento se crea sin embedding; se puede reindexar después
            context.Response.Headers.Append("X-Warning", "Embedding no generado: " + ex.Message);
        }

        var doc = new Document
        {
            Id            = Guid.NewGuid(),
            TenantId      = tenantId,
            Title         = request.Title,
            Content       = request.Content,
            Visibility    = request.Visibility,
            CategoryId    = request.CategoryId,
            RelatedRoleId = request.RelatedRoleId,
            Tags          = request.Tags ?? [],
            PriorityLevel = request.PriorityLevel,
            Embedding     = embedding,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };

        await repo.AddAsync(doc, ct);
        return Results.Created($"/documents/{doc.Id}", ToResponse(doc));
    }

    private static async Task<IResult> UpdateDocumentAsync(
        Guid id,
        UpdateDocumentRequest request,
        IDocumentRepository repo,
        IVectorSearchService vectorSearch,
        HttpContext context,
        CancellationToken ct = default)
    {
        var doc = await repo.GetByIdAsync(id, ct);
        if (doc is null)
            return Results.NotFound(new { error = "Documento no encontrado." });

        var tenantId       = (Guid)context.Items["TenantId"]!;
        var contentChanged = doc.Content != request.Content || doc.Title != request.Title;

        doc.Title         = request.Title;
        doc.Content       = request.Content;
        doc.Visibility    = request.Visibility;
        doc.CategoryId    = request.CategoryId;
        doc.RelatedRoleId = request.RelatedRoleId;
        doc.Tags          = request.Tags ?? [];
        doc.PriorityLevel = request.PriorityLevel;
        doc.IsActive      = request.IsActive;
        doc.UpdatedAt     = DateTime.UtcNow;

        // Regenerar embedding si cambió el contenido
        if (contentChanged)
        {
            try
            {
                var raw = await vectorSearch.GetEmbeddingAsync(
                    tenantId, $"{doc.Title}\n{doc.Content}", ct);
                doc.Embedding = new Vector(raw);
            }
            catch { /* mantener embedding anterior si falla */ }
        }

        await repo.UpdateAsync(doc, ct);
        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(doc));
    }

    private static async Task<IResult> DeactivateDocumentAsync(
        Guid id, IDocumentRepository repo, CancellationToken ct = default)
    {
        var doc = await repo.GetByIdAsync(id, ct);
        if (doc is null)
            return Results.NotFound(new { error = "Documento no encontrado." });

        doc.IsActive  = false;
        doc.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(doc, ct);
        await repo.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ReindexDocumentAsync(
        Guid id,
        IDocumentRepository repo,
        IVectorSearchService vectorSearch,
        HttpContext context,
        CancellationToken ct = default)
    {
        var doc = await repo.GetByIdAsync(id, ct);
        if (doc is null)
            return Results.NotFound(new { error = "Documento no encontrado." });

        var tenantId = (Guid)context.Items["TenantId"]!;
        var raw = await vectorSearch.GetEmbeddingAsync(
            tenantId, $"{doc.Title}\n{doc.Content}", ct);

        doc.Embedding = new Vector(raw);
        doc.UpdatedAt = DateTime.UtcNow;

        await repo.UpdateAsync(doc, ct);
        await repo.SaveChangesAsync(ct);
        return Results.Ok(new { message = "Embedding regenerado.", documentId = doc.Id });
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static DocumentResponse ToResponse(Document d) =>
        new(d.Id, d.Title, d.Content, d.Visibility,
            d.CategoryId, d.RelatedRoleId, d.Tags,
            d.PriorityLevel, d.IsActive,
            HasEmbedding: d.Embedding is not null,
            d.CreatedAt, d.UpdatedAt);
}
