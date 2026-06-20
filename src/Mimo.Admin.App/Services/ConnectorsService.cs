using Mimo.Admin.Api.Sdk;
using Mimo.Admin.Api.Sdk.Models;

namespace Mimo.Admin.App.Services;

/// <summary>Gestión de conectores de IA (endpoint /ai/connectors). Las respuestas no exponen la API key.</summary>
public sealed class ConnectorsService(MimoAdminApiClient api)
{
    public async Task<List<AiConnectorResponse>> ListAsync(CancellationToken ct = default)
    {
        var result = await api.Ai.Connectors.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<AiConnectorResponse?> CreateAsync(CreateAiConnectorRequest request, CancellationToken ct = default) =>
        api.Ai.Connectors.PostAsync(request, cancellationToken: ct);

    public Task<AiConnectorResponse?> UpdateAsync(Guid id, UpdateAiConnectorRequest request, CancellationToken ct = default) =>
        api.Ai.Connectors.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task ActivateAsync(Guid id, CancellationToken ct = default) =>
        api.Ai.Connectors.Activate.PostAsync(rc => rc.QueryParameters.Id = id, ct);
}
