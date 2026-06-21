using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Reglas de automatización (#24) por encima del cliente Kiota.
/// </summary>
public sealed class AutomationRulesService(MimoApiClient api)
{
    public async Task<List<AutomationRuleResponse>> ListAsync(CancellationToken ct = default)
    {
        var result = await api.AutomationRules.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<AutomationRuleResponse?> CreateAsync(CreateAutomationRuleRequest request, CancellationToken ct = default) =>
        api.AutomationRules.PostAsync(request, cancellationToken: ct);

    public Task<AutomationRuleResponse?> UpdateAsync(Guid id, UpdateAutomationRuleRequest request, CancellationToken ct = default) =>
        api.AutomationRules.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        api.AutomationRules.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
