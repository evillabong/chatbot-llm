using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Operaciones sobre funcionarios (endpoint /agents) por encima del cliente Kiota.
/// Las páginas usan este servicio; no llaman HTTP directamente.
/// </summary>
public sealed class AgentsService(MimoApiClient api)
{
    public async Task<List<AgentResponse>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var result = await api.Agents.GetAsync(rc =>
        {
            if (isActive is not null) rc.QueryParameters.IsActive = isActive;
        }, ct);
        return result ?? [];
    }

    public Task<AgentResponse?> CreateAsync(CreateAgentRequest request, CancellationToken ct = default) =>
        api.Agents.PostAsync(request, cancellationToken: ct);

    public Task<AgentResponse?> UpdateAsync(Guid id, UpdateAgentRequest request, CancellationToken ct = default) =>
        api.Agents.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeactivateAsync(Guid id, CancellationToken ct = default) =>
        api.Agents.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
