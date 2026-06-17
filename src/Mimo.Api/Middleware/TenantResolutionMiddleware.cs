using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mimo.Core.Interfaces;
using Mimo.Core.MultiTenancy;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Middleware;

/// <summary>
/// Resuelve el tenant de cada petición y establece el search_path de PostgreSQL
/// para que todas las consultas subsiguientes operen sobre el esquema correcto.
///
/// Resolución (ver ADR 0008): en peticiones AUTENTICADAS el tenant lo dicta el token
/// (claim tenant_slug); el slug provisto por el cliente (header X-Tenant-Slug o subdominio)
/// solo aplica en flujos anónimos. Si una petición autenticada pide un tenant distinto al
/// de su token → 403 (intento de acceso cross-tenant).
///
/// El tenant resuelto se cachea en IMemoryCache durante 5 minutos.
///
/// Si no se puede resolver el tenant → 400.
/// Si hay conflicto token vs cliente → 403.
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

    public async Task InvokeAsync(HttpContext context, GlobalDbContext globalDb, ITenantSchemaProvider schemaProvider)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var normalizedPath = path.ToLowerInvariant();

        // Rutas no asociadas a un tenant: health/ready y el documento OpenAPI (solo Development).
        if (BypassPaths.Contains(normalizedPath) || normalizedPath.StartsWith("/openapi"))
        {
            await next(context);
            return;
        }

        var tokenSlug  = context.User?.FindFirst("tenant_slug")?.Value;
        var clientSlug = ResolveClientSlug(context);
        var resolution = TenantResolver.Resolve(tokenSlug, clientSlug);

        if (resolution.Conflict)
        {
            logger.LogWarning(
                "Intento cross-tenant: token={TokenSlug} cliente={ClientSlug}", tokenSlug, clientSlug);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "El tenant solicitado no coincide con el de tu sesión." });
            return;
        }

        var slug = resolution.Slug;

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

        // Fijar el esquema del tenant para esta petición. El SearchPathConnectionInterceptor
        // aplica el search_path en cada apertura de conexión de TenantDbContext (fiable con pooling).
        var schemaName = $"tenant_{SlugRegex().Replace(slug.Replace("-", "_"), "")}";
        schemaProvider.Schema = schemaName;

        // Exponer el tenant resuelto al resto del pipeline (leído vía TenantHttpContextExtensions)
        context.Items[TenantHttpContextExtensions.TenantIdKey]            = tenant.Id;
        context.Items[TenantHttpContextExtensions.TenantSlugKey]          = tenant.Slug;
        context.Items[TenantHttpContextExtensions.TenantConfigurationKey] = tenant.Configuration;
        context.Items[TenantHttpContextExtensions.TenantSchemaKey]        = schemaName;

        logger.LogDebug("Tenant resuelto: {Slug} (schema: {Schema})", slug, schemaName);

        await next(context);
    }

    /// <summary>Solo letras minúsculas y dígitos (ya reemplazados los guiones por _).</summary>
    [GeneratedRegex(@"[^a-z0-9_]")]
    private static partial Regex SlugRegex();

    /// <summary>
    /// Slug provisto por el cliente: header X-Tenant-Slug y, en su defecto, el subdominio.
    /// La decisión de si este slug se usa o se rechaza la toma <see cref="TenantResolver"/>
    /// según haya o no un token (binding al principal).
    /// </summary>
    private static string? ResolveClientSlug(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var headerSlug) &&
            !string.IsNullOrWhiteSpace(headerSlug))
            return headerSlug.ToString();

        var parts = context.Request.Host.Host.Split('.');
        if (parts.Length >= 3)
            return parts[0];

        return null;
    }
}
