using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Lee y actualiza la configuración del tenant autenticado (endpoint /tenant/configuration).
/// </summary>
public sealed class TenantConfigService(MimoApiClient api)
{
    public Task<TenantConfiguration?> GetAsync(CancellationToken ct = default) =>
        api.Tenant.Configuration.GetAsync(cancellationToken: ct);

    public Task<TenantConfiguration?> UpdateAsync(TenantConfiguration config, CancellationToken ct = default) =>
        api.Tenant.Configuration.PutAsync(config, cancellationToken: ct);
}
