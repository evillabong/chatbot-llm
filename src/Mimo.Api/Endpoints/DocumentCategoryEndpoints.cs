using Microsoft.EntityFrameworkCore;
using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Document;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Endpoints;

/// <summary>
/// CRUD de categorías documentales (#17). Lectura para cualquier funcionario (poblar el desplegable
/// de `/conocimiento`); creación/borrado para TenantAdmin. Opera sobre el esquema del tenant.
/// </summary>
public static class DocumentCategoryEndpoints
{
    public static IEndpointRouteBuilder MapDocumentCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/document-categories")
            .WithTags("DocumentCategories")
            .RequireAuthorization(MimoAuthorization.Policies.Agent);

        group.MapGet("/", ListAsync)
            .WithName("ListDocumentCategories")
            .WithSummary("Lista las categorías documentales del tenant.")
            .Produces<List<DocumentCategoryResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateDocumentCategory")
            .WithSummary("Crea una categoría documental.")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin)
            .Produces<DocumentCategoryResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/", DeleteAsync)
            .WithName("DeleteDocumentCategory")
            .WithSummary("Elimina una categoría documental si no está en uso (query: id).")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(TenantDbContext db, CancellationToken ct = default)
    {
        var items = await db.DocumentCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new DocumentCategoryResponse(c.Id, c.Name, c.ParentCategoryId))
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateAsync(
        CreateDocumentCategoryRequest request, TenantDbContext db, HttpContext context, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await db.DocumentCategories.AnyAsync(c => c.Name == name, ct))
            return Results.Conflict(new { error = "Ya existe una categoría con ese nombre." });

        if (request.ParentCategoryId is { } parentId &&
            !await db.DocumentCategories.AnyAsync(c => c.Id == parentId, ct))
            return Results.Conflict(new { error = "La categoría padre no existe." });

        var category = new DocumentCategory
        {
            Id               = Guid.NewGuid(),
            TenantId         = context.GetTenantId(),
            Name             = name,
            ParentCategoryId = request.ParentCategoryId
        };
        db.DocumentCategories.Add(category);
        await db.SaveChangesAsync(ct);

        return Results.Created("/document-categories",
            new DocumentCategoryResponse(category.Id, category.Name, category.ParentCategoryId));
    }

    private static async Task<IResult> DeleteAsync(Guid id, TenantDbContext db, CancellationToken ct = default)
    {
        var category = await db.DocumentCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return Results.NotFound(new { error = "Categoría no encontrada." });

        // No borrar si hay documentos o subcategorías que la referencian.
        if (await db.Documents.AnyAsync(d => d.CategoryId == id, ct) ||
            await db.DocumentCategories.AnyAsync(c => c.ParentCategoryId == id, ct))
            return Results.Conflict(new { error = "La categoría está en uso (documentos o subcategorías)." });

        db.DocumentCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
