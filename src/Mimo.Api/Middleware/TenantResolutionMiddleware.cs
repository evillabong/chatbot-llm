using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Middleware;

/// <summary>
/// Resuelve el tenant de cada petición y establece el search_path de PostgreSQL
/// para que todas las consultas subsiguientes operen sobre el esquema correcto.
///
/// Orden de resolución:
///   1. Header X-Tenant-Slug
///   2. Subdominio del host (ej: municipio.mimo.app → slug = "municipio")
///   3. Claim tenant_slug del JWT
///
/// El tenant resuelto se cachea en IMemoryCache durante 5 minutos para evitar
/// una consulta a GlobalDbContext en cada request.
///
/// Si no se puede resolver el tenant → 400.
/// Si el tenant no existe → 404.
/// Si está inactivo → 403.
/// </summary>
public partial class TenantResolutionMiddleware(
    RequestDelegate next,
    IMemoryCache cache,
    ILogger<TenantResolutionMiddleware> logger)
{
    private static readonly HashSet<string> BypassPaths = ["/health", "/ready"];
    private static readonly TimeSpan        CacheTtl    = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext context, GlobalDbContext globalDb, TenantDbContext tenantDb)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (BypassPaths.Contains(path.ToLowerInvariant()))
        {
            await next(context);
            return;
        }

        var slug = ResolveSlug(context);

        if (string.IsNullOrWhiteSpace(slug))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "No se pudo determinar el tenant de la solicitud." });
            return;
        }

        // Cachear el tenant para evitar una consulta a GlobalDbContext en cada request.
        // TTL corto (5 min) para reflejar cambios de estado (desactivación de tenant) con rapidez.
        var cacheKey = $"mimo:tenant:{slug}";
        var tenant   = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await globalDb.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug, context.RequestAborted);
        });

        if (tenant is null)
        {
            // Evitar que un slug inválido quede cacheado como null indefinidamente
            cache.Remove(cacheKey);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant no encontrado." });
            return;
        }

        if (!tenant.IsActive)
        {
            // Forzar re-consulta en el próximo request para detectar reactivación
            cache.Remove(cacheKey);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant inactivo." });
            return;
        }

        // Establecer search_path en TenantDbContext para aislar las consultas al esquema del tenant.
        var schemaName = $"tenant_{SlugRegex().Replace(slug.Replace("-", "_"), "")}";
        await tenantDb.Database.ExecuteSqlAsync(
            $"SET search_path TO {schemaName}, public",
            context.RequestAborted);

        // Exponer el tenant resuelto al resto del pipeline
        context.Items["TenantId"]            = tenant.Id;
        context.Items["TenantSlug"]          = tenant.Slug;
        context.Items["TenantConfiguration"] = tenant.Configuration;

        logger.LogDebug("Tenant resuelto: {Slug} (schema: {Schema})", slug, schemaName);

        await next(context);
    }

    /// <summary>Solo letras minúsculas y dígitos (ya reemplazados los guiones por _).</summary>
    [GeneratedRegex(@"[^a-z0-9_]")]
    private static partial Regex SlugRegex();

    private static string? ResolveSlug(HttpContext context)
    {
        // 1. Header explícito
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var headerSlug) &&
            !string.IsNullOrWhiteSpace(headerSlug))
            return headerSlug.ToString().ToLowerInvariant();

        // 2. Subdominio (ej: municipio.mimo.app)
        var host  = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3)
            return parts[0].ToLowerInvariant();

        // 3. Claim del JWT
        var tenantClaim = context.User?.FindFirst("tenant_slug")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantClaim))
            return tenantClaim.ToLowerInvariant();

        return null;
    }
}
