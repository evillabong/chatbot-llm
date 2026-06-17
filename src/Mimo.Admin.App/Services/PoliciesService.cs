using Mimo.Admin.Api.Sdk;
using Mimo.Admin.Api.Sdk.Models;

namespace Mimo.Admin.App.Services;

/// <summary>Gestión de políticas de IA por plan (endpoint /ai/plan-policies).</summary>
public sealed class PoliciesService(MimoAdminApiClient api)
{
    public async Task<List<AiPlanPolicyResponse>> ListAsync(CancellationToken ct = default)
    {
        var result = await api.Ai.PlanPolicies.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<AiPlanPolicyResponse?> UpsertAsync(string planCode, UpsertAiPlanPolicyRequest request, CancellationToken ct = default) =>
        api.Ai.PlanPolicies.PutAsync(request, rc => rc.QueryParameters.PlanCode = planCode, ct);

    public Task DeleteAsync(string planCode, CancellationToken ct = default) =>
        api.Ai.PlanPolicies.DeleteAsync(rc => rc.QueryParameters.PlanCode = planCode, ct);
}
