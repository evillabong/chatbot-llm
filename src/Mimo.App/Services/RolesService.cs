using Mimo.ApiClient.MimoApi;
using Mimo.ApiClient.MimoApi.Models;

namespace Mimo.App.Services;

/// <summary>Operaciones de lectura de roles/departamentos (endpoint /roles).</summary>
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
}
