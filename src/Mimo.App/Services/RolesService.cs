using Mimo.ApiClient.MimoApi;
using Mimo.ApiClient.MimoApi.Models;

namespace Mimo.App.Services;

/// <summary>Operaciones sobre roles/departamentos (endpoint /roles) por encima del cliente Kiota.</summary>
public sealed class RolesService(MimoApiClient api)
{
    public async Task<List<RoleResponse>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var result = await api.Roles.GetAsync(rc =>
        {
            if (isActive is not null) rc.QueryParameters.IsActive = isActive;
        }, ct);
        return result ?? [];
    }

    public Task<RoleResponse?> CreateAsync(CreateRoleRequest request, CancellationToken ct = default) =>
        api.Roles.PostAsync(request, cancellationToken: ct);

    public Task<RoleResponse?> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default) =>
        api.Roles.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeactivateAsync(Guid id, CancellationToken ct = default) =>
        api.Roles.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
