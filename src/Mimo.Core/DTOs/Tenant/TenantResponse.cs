namespace Mimo.Core.DTOs.Tenant;

/// <summary>
/// Representación pública de un tenant para las respuestas de la API.
/// </summary>
public record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string Plan,
    bool IsActive,
    DateTime CreatedAt
);
