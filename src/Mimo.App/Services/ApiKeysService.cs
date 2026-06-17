using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Administración de API keys de interoperabilidad del tenant (endpoint /integration/api-keys).
/// La clave en claro solo llega en la respuesta de creación.
/// </summary>
public sealed class ApiKeysService(MimoApiClient api)
{
    public async Task<List<ApiKeyResponse>> ListAsync(CancellationToken ct = default)
    {
        var result = await api.Integration.ApiKeys.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<CreatedApiKeyResponse?> CreateAsync(string name, CancellationToken ct = default) =>
        api.Integration.ApiKeys.PostAsync(new CreateApiKeyRequest { Name = name }, cancellationToken: ct);

    public Task RevokeAsync(Guid id, CancellationToken ct = default) =>
        api.Integration.ApiKeys.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
