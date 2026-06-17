using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.Models.Configuration;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Configuración del tenant autenticado (parámetros de atención, asignación, encuestas,
/// horario, personalización de canales…). El tenant se resuelve por el token (ADR 0008):
/// el TenantAdmin solo lee/edita la configuración de SU tenant. Requiere rol Administrador.
/// La configuración vive como JSONB en el catálogo global (public.tenants).
/// </summary>
public static class TenantConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapTenantConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tenant")
            .WithTags("Tenant")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/configuration", GetConfiguration)
            .WithName("GetTenantConfiguration")
            .WithSummary("Obtiene la configuración del tenant autenticado.")
            .Produces<TenantConfiguration>();

        group.MapPut("/configuration", UpdateConfigurationAsync)
            .WithName("UpdateTenantConfiguration")
            .WithSummary("Actualiza la configuración del tenant autenticado.")
            .Produces<TenantConfiguration>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult GetConfiguration(HttpContext context) =>
        Results.Ok(context.GetTenantConfiguration() ?? new TenantConfiguration());

    private static async Task<IResult> UpdateConfigurationAsync(
        TenantConfiguration request,
        HttpContext context,
        GlobalDbContext globalDb,
        IMemoryCache cache,
        CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();

        var tenant = await globalDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        // Asignar un objeto nuevo (no mutar el existente): la columna usa conversión de valor,
        // por lo que el cambio se detecta por reemplazo de referencia.
        tenant.Configuration = request;
        tenant.UpdatedAt     = DateTime.UtcNow;
        await globalDb.SaveChangesAsync(ct);

        // Invalidar la caché del TenantResolutionMiddleware para reflejar el cambio de inmediato.
        cache.Remove($"mimo:tenant:{tenant.Slug}");

        return Results.Ok(tenant.Configuration);
    }
}
