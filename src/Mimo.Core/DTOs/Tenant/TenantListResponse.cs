namespace Mimo.Core.DTOs.Tenant;

/// <summary>
/// Respuesta paginada de la lista de tenants.
/// </summary>
public record TenantListResponse(
    IReadOnlyList<TenantResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);
