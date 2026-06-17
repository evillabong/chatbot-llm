using Mimo.Admin.Api.Sdk;
using Mimo.Admin.Api.Sdk.Models;

namespace Mimo.Admin.App.Services;

/// <summary>Gestión del catálogo de planes (endpoint /plans de la admin API).</summary>
public sealed class PlansService(MimoAdminApiClient api)
{
    public async Task<List<PlanResponse>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var result = await api.Plans.GetAsync(rc =>
        {
            if (isActive is not null) rc.QueryParameters.IsActive = isActive;
        }, ct);
        return result ?? [];
    }

    public Task<PlanResponse?> CreateAsync(CreatePlanRequest request, CancellationToken ct = default) =>
        api.Plans.PostAsync(request, cancellationToken: ct);

    public Task<PlanResponse?> UpdateAsync(string code, UpdatePlanRequest request, CancellationToken ct = default) =>
        api.Plans.PutAsync(request, rc => rc.QueryParameters.Code = code, ct);

    public Task DeactivateAsync(string code, CancellationToken ct = default) =>
        api.Plans.DeleteAsync(rc => rc.QueryParameters.Code = code, ct);
}
