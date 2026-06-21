using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Oportunidades de venta (#26) por encima del cliente Kiota.
/// </summary>
public sealed class OpportunitiesService(MimoApiClient api)
{
    public async Task<List<OpportunityResponse>> ListAsync(int? stage = null, Guid? assignedAgentId = null, CancellationToken ct = default)
    {
        var result = await api.Opportunities.GetAsync(rc =>
        {
            if (stage is not null) rc.QueryParameters.Stage = stage;
            if (assignedAgentId is not null) rc.QueryParameters.AssignedAgentId = assignedAgentId;
        }, ct);
        return result ?? [];
    }

    public Task<OpportunityResponse?> CreateAsync(CreateOpportunityRequest request, CancellationToken ct = default) =>
        api.Opportunities.PostAsync(request, cancellationToken: ct);

    public Task<OpportunityResponse?> UpdateAsync(Guid id, UpdateOpportunityRequest request, CancellationToken ct = default) =>
        api.Opportunities.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        api.Opportunities.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
