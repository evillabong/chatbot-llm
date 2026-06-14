using Mimo.Core.Models.Configuration;

namespace Mimo.Api.Middleware;

/// <summary>
/// Acceso tipado al tenant resuelto por <see cref="TenantResolutionMiddleware"/>.
///
/// El tenant se almacena en HttpContext.Items por el middleware (fuente única que también
/// fija el search_path de PostgreSQL). Estos accesores centralizan la lectura y evitan
/// el casteo manual disperso por los endpoints.
/// </summary>
public static class TenantHttpContextExtensions
{
    // Claves internas; nadie fuera de aquí debería referenciarlas como string.
    internal const string TenantIdKey            = "TenantId";
    internal const string TenantSlugKey          = "TenantSlug";
    internal const string TenantConfigurationKey = "TenantConfiguration";

    /// <summary>Id del tenant resuelto. Lanza si el middleware no lo estableció.</summary>
    public static Guid GetTenantId(this HttpContext context) =>
        context.Items[TenantIdKey] is Guid id
            ? id
            : throw new InvalidOperationException(
                "El tenant no fue resuelto para esta petición (¿falta TenantResolutionMiddleware o es una ruta bypass?).");

    /// <summary>Slug del tenant resuelto. Lanza si el middleware no lo estableció.</summary>
    public static string GetTenantSlug(this HttpContext context) =>
        context.Items[TenantSlugKey] as string
            ?? throw new InvalidOperationException(
                "El tenant no fue resuelto para esta petición (¿falta TenantResolutionMiddleware o es una ruta bypass?).");

    /// <summary>Configuración del tenant resuelto, si está disponible.</summary>
    public static TenantConfiguration? GetTenantConfiguration(this HttpContext context) =>
        context.Items[TenantConfigurationKey] as TenantConfiguration;
}
