using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Middleware;

/// <summary>
/// Resuelve el tenant de cada petición y establece el search_path de PostgreSQL
/// para que todas las consultas subsiguientes operen sobre el esquema correcto.
///
/// Orden de resolución:
///   1. Header X-Tenant-Slug
///   2. Subdominio del host (ej: municipio.mimo.app → slug = "municipio")
///   3. Claim TenantId del JWT
///
/// Si no se puede resolver el tenant, retorna 400. Si el tenant no existe o está
/// inactivo, retorna 404 / 403.
/// </summary>
public partial class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger)
{
    // Rutas que no requieren tenant (health check, etc.)
    private static readonly HashSet<string> BypassPaths = ["/health", "/ready"];

    public async Task InvokeAsync(HttpContext context, GlobalDbContext globalDb, TenantDbContext tenantDb)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Rutas exentas de resolución de tenant
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

        // Consultar en GlobalDbContext (esquema public — no depende del tenant)
        var tenant = await globalDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug, context.RequestAborted);

        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant no encontrado." });
            return;
        }

        if (!tenant.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant inactivo." });
            return;
        }

        // Establecer search_path en TenantDbContext para aislar las consultas al esquema del tenant.
        // El slug ya fue validado contra la BD; se sanitiza adicionalmente para evitar inyección.
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

    /// <summary>Expresión regular para sanitizar el schema: solo letras minúsculas y dígitos.</summary>
    [GeneratedRegex(@"[^a-z0-9_]")]
    private static partial Regex SlugRegex();

    /// <summary>
    /// Intenta extraer el slug del tenant usando los mecanismos disponibles.
    /// </summary>
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
