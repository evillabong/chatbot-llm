namespace Mimo.Core.MultiTenancy;

/// <summary>Resultado de resolver el slug del tenant para una petición.</summary>
/// <param name="Slug">Slug efectivo, o null si no se pudo determinar.</param>
/// <param name="Conflict">
/// True si una petición autenticada pidió un tenant distinto al de su token
/// (intento de acceso cross-tenant): el llamador debe responder 403.
/// </param>
public readonly record struct TenantSlugResolution(string? Slug, bool Conflict);

/// <summary>
/// Regla pura de resolución del tenant (ver ADR 0008). Aísla la decisión de seguridad
/// del transporte HTTP para poder probarla de forma aislada.
///
/// Principio: en una petición autenticada el tenant lo dicta el token (no el cliente).
/// El slug provisto por el cliente (header/subdominio) solo manda en flujos anónimos.
/// </summary>
public static class TenantResolver
{
    /// <param name="tokenSlug">Slug del claim del token (null si la petición es anónima).</param>
    /// <param name="clientSlug">Slug provisto por el cliente: header o subdominio (puede ser null).</param>
    public static TenantSlugResolution Resolve(string? tokenSlug, string? clientSlug)
    {
        tokenSlug  = Normalize(tokenSlug);
        clientSlug = Normalize(clientSlug);

        // Petición autenticada: el token manda.
        if (tokenSlug is not null)
        {
            // Si el cliente además pide explícitamente OTRO tenant, es un intento cross-tenant.
            if (clientSlug is not null && clientSlug != tokenSlug)
                return new TenantSlugResolution(null, Conflict: true);

            return new TenantSlugResolution(tokenSlug, Conflict: false);
        }

        // Petición anónima (ciudadano/webhook): se resuelve por subdominio/header/config.
        return new TenantSlugResolution(clientSlug, Conflict: false);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
