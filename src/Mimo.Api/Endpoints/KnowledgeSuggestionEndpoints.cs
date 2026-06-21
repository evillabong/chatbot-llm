using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Knowledge;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Pgvector;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Bandeja de curación de sugerencias de conocimiento (#22, Fase 1 — HITL). Un TenantAdmin revisa
/// borradores candidatos y, al aprobar, se publican como <see cref="Document"/> (alimentando el RAG).
/// Las sugerencias se crean manualmente o desde un vacío detectado; el worker batch que las generará
/// por IA es un corte posterior.
/// </summary>
public static class KnowledgeSuggestionEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeSuggestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/knowledge/suggestions")
            .WithTags("KnowledgeSuggestions")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListKnowledgeSuggestions")
            .WithSummary("Lista las sugerencias de conocimiento (query opcional: status 0=pendiente,1=aprobada,2=descartada).")
            .Produces<List<KnowledgeSuggestionResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateKnowledgeSuggestion")
            .WithSummary("Crea una sugerencia de conocimiento candidata.")
            .Produces<KnowledgeSuggestionResponse>(StatusCodes.Status201Created);

        group.MapPost("/approve", ApproveAsync)
            .WithName("ApproveKnowledgeSuggestion")
            .WithSummary("Aprueba una sugerencia y la publica como documento (query: id).")
            .Produces<KnowledgeSuggestionResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/discard", DiscardAsync)
            .WithName("DiscardKnowledgeSuggestion")
            .WithSummary("Descarta una sugerencia pendiente (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IKnowledgeSuggestionRepository repo, int? status = null, CancellationToken ct = default)
    {
        KnowledgeSuggestionStatus? filter = status is not null && Enum.IsDefined(typeof(KnowledgeSuggestionStatus), status.Value)
            ? (KnowledgeSuggestionStatus)status.Value
            : null;

        var items = await repo.ListAsync(filter, ct);
        return Results.Ok(items.Select(ToResponse).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateKnowledgeSuggestionRequest request,
        IKnowledgeSuggestionRepository repo,
        HttpContext context,
        CancellationToken ct = default)
    {
        var suggestion = new KnowledgeSuggestion
        {
            Id             = Guid.NewGuid(),
            TenantId       = context.GetTenantId(),
            SourceSignalId = request.SourceSignalId,
            DraftTitle     = request.DraftTitle.Trim(),
            DraftContent   = request.DraftContent,
            Status         = KnowledgeSuggestionStatus.Pending,
            CreatedAt      = DateTime.UtcNow
        };
        await repo.AddAsync(suggestion, ct);
        return Results.Created("/knowledge/suggestions", ToResponse(suggestion));
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        IKnowledgeSuggestionRepository repo,
        IDocumentRepository documents,
        IVectorSearchService vectorSearch,
        HttpContext context,
        CancellationToken ct = default)
    {
        var suggestion = await repo.GetByIdAsync(id, ct);
        if (suggestion is null)
            return Results.NotFound(new { error = "Sugerencia no encontrada." });
        if (suggestion.Status != KnowledgeSuggestionStatus.Pending)
            return Results.Conflict(new { error = "La sugerencia ya fue revisada." });

        var tenantId = context.GetTenantId();

        // Embedding del contenido (degrada sin LLM, igual que la creación de documentos; se reindexa luego).
        Vector? embedding = null;
        try
        {
            var raw = await vectorSearch.GetEmbeddingAsync(
                tenantId, $"{suggestion.DraftTitle}\n{suggestion.DraftContent}", ct);
            embedding = new Vector(raw);
        }
        catch (Exception ex)
        {
            context.Response.Headers.Append("X-Warning", "Embedding no generado: " + ex.Message);
        }

        var document = new Document
        {
            Id         = Guid.NewGuid(),
            TenantId   = tenantId,
            Title      = suggestion.DraftTitle,
            Content    = suggestion.DraftContent,
            Visibility = VisibilityLevel.Public,
            Tags       = [],
            Embedding  = embedding,
            IsActive   = true,
            CreatedAt  = DateTime.UtcNow
        };
        await documents.AddAsync(document, ct);

        suggestion.Status              = KnowledgeSuggestionStatus.Approved;
        suggestion.PublishedDocumentId = document.Id;
        suggestion.ReviewedAt          = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(suggestion));
    }

    private static async Task<IResult> DiscardAsync(
        Guid id, IKnowledgeSuggestionRepository repo, CancellationToken ct = default)
    {
        var suggestion = await repo.GetByIdAsync(id, ct);
        if (suggestion is null)
            return Results.NotFound(new { error = "Sugerencia no encontrada." });
        if (suggestion.Status != KnowledgeSuggestionStatus.Pending)
            return Results.Conflict(new { error = "La sugerencia ya fue revisada." });

        suggestion.Status     = KnowledgeSuggestionStatus.Discarded;
        suggestion.ReviewedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static KnowledgeSuggestionResponse ToResponse(KnowledgeSuggestion s) => new(
        s.Id, s.SourceSignalId, s.DraftTitle, s.DraftContent, s.Status, s.PublishedDocumentId, s.CreatedAt, s.ReviewedAt);
}
