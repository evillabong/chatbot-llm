using Mimo.Admin.Api.Sdk;
using Mimo.Admin.Api.Sdk.Models;

namespace Mimo.Admin.App.Services;

/// <summary>Gestión de tenants (endpoint /tenants de la admin API) sobre el cliente Kiota.</summary>
public sealed class TenantsService(MimoAdminApiClient api)
{
    public Task<PagedResultOfTenantResponse?> ListAsync(int page = 1, int pageSize = 50, bool? isActive = null, CancellationToken ct = default) =>
        api.Tenants.GetAsync(rc =>
        {
            rc.QueryParameters.Page = page;
            rc.QueryParameters.PageSize = pageSize;
            if (isActive is not null) rc.QueryParameters.IsActive = isActive;
        }, ct);

    public Task<TenantResponse?> CreateAsync(CreateTenantRequest request, CancellationToken ct = default) =>
        api.Tenants.PostAsync(request, cancellationToken: ct);

    public Task<TenantResponse?> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken ct = default) =>
        api.Tenants.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeactivateAsync(Guid id, CancellationToken ct = default) =>
        api.Tenants.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
