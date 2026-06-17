using Microsoft.EntityFrameworkCore;
using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Integration;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Interoperabilidad del tenant: administración de API keys (corte 1).
/// Las claves viven en el esquema global con TenantId; el TenantAdmin solo gestiona las de
/// SU tenant (resuelto por el token, ADR 0008). La clave en claro se devuelve solo al crearla.
/// </summary>
public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/integration/api-keys")
            .WithTags("Integration")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListApiKeys")
            .WithSummary("Lista las API keys del tenant (sin exponer la clave).")
            .Produces<List<ApiKeyResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateApiKey")
            .WithSummary("Genera una API key; la clave en claro se devuelve UNA sola vez.")
            .Produces<CreatedApiKeyResponse>(StatusCodes.Status201Created);

        group.MapDelete("/", RevokeAsync)
            .WithName("RevokeApiKey")
            .WithSummary("Revoca una API key del tenant (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        HttpContext context, GlobalDbContext db, CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();
        var keys = await db.ApiKeys
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyResponse(k.Id, k.Name, k.Prefix, k.IsActive, k.CreatedAt, k.LastUsedAt))
            .ToListAsync(ct);
        return Results.Ok(keys);
    }

    private static async Task<IResult> CreateAsync(
        CreateApiKeyRequest request,
        HttpContext context,
        GlobalDbContext db,
        IApiKeyService apiKeys,
        CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();
        var generated = apiKeys.Generate();

        var entity = new ApiKey
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenantId,
            Name      = request.Name,
            Prefix    = generated.Prefix,
            KeyHash   = generated.Hash,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };

        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync(ct);

        // La clave en claro se devuelve solo aquí; no se puede recuperar después.
        return Results.Created("/integration/api-keys",
            new CreatedApiKeyResponse(entity.Id, entity.Name, entity.Prefix, generated.PlainKey, entity.CreatedAt));
    }

    private static async Task<IResult> RevokeAsync(
        Guid id, HttpContext context, GlobalDbContext db, CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId, ct);
        if (key is null)
            return Results.NotFound(new { error = "API key no encontrada." });

        key.IsActive  = false;
        key.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
