namespace Mimo.Core.DTOs.Integration;

/// <summary>
/// Identidad devuelta por la API de integración (<c>GET /integration/v1/me</c>): confirma a un
/// sistema externo qué organización y qué API key está usando. Sirve como verificación de credenciales.
/// </summary>
public record IntegrationIdentityResponse(
    string TenantSlug,
    string TenantName,
    string Plan,
    string ApiKeyName
);
