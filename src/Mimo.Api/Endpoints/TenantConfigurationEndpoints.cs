using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.Interfaces;
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

    private static IResult GetConfiguration(HttpContext context)
    {
        var cfg = context.GetTenantConfiguration() ?? new TenantConfiguration();
        // No exponer secretos de canal (write-only, igual que la API key del conector de IA, ADR 0010).
        // Se devuelve una copia profunda para no mutar el objeto cacheado por el middleware.
        var safe = Clone(cfg);
        if (safe.ChannelCustomization is not null)
            safe.ChannelCustomization.TelegramBotToken = null;
        return Results.Ok(safe);
    }

    private static async Task<IResult> UpdateConfigurationAsync(
        TenantConfiguration request,
        HttpContext context,
        GlobalDbContext globalDb,
        ISecretProtector secretProtector,
        IMemoryCache cache,
        CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();

        var tenant = await globalDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        // Cifrar en reposo los secretos de canal (ADR 0010). Semántica write-only: si el cliente
        // envía el token vacío (la lectura lo enmascara), se conserva el valor cifrado existente;
        // si envía uno nuevo, se cifra. Así un guardado de la UI no borra el token.
        request.ChannelCustomization ??= new ChannelCustomizationConfig();
        var incomingToken = request.ChannelCustomization.TelegramBotToken;
        var existingToken = tenant.Configuration?.ChannelCustomization?.TelegramBotToken;
        request.ChannelCustomization.TelegramBotToken = string.IsNullOrWhiteSpace(incomingToken)
            ? existingToken
            : secretProtector.Protect(incomingToken);

        // Asignar un objeto nuevo (no mutar el existente): la columna usa conversión de valor,
        // por lo que el cambio se detecta por reemplazo de referencia.
        tenant.Configuration = request;
        tenant.UpdatedAt     = DateTime.UtcNow;
        await globalDb.SaveChangesAsync(ct);

        // Invalidar la caché del TenantResolutionMiddleware para reflejar el cambio de inmediato.
        cache.Remove($"mimo:tenant:{tenant.Slug}");

        // Responder enmascarando el secreto (no devolverlo en claro ni cifrado).
        var safe = Clone(tenant.Configuration);
        if (safe.ChannelCustomization is not null)
            safe.ChannelCustomization.TelegramBotToken = null;
        return Results.Ok(safe);
    }

    /// <summary>Copia profunda vía JSON (los config son POCOs); evita mutar el objeto cacheado.</summary>
    private static TenantConfiguration Clone(TenantConfiguration cfg) =>
        JsonSerializer.Deserialize<TenantConfiguration>(JsonSerializer.Serialize(cfg)) ?? new TenantConfiguration();
}
