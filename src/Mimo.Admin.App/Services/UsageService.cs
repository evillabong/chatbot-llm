using Mimo.Admin.Api.Sdk;
using Mimo.Admin.Api.Sdk.Models;

namespace Mimo.Admin.App.Services;

/// <summary>Resumen de uso de IA por tenant (endpoint /ai/usage/summary de la admin API).</summary>
public sealed class UsageService(MimoAdminApiClient api)
{
    public async Task<List<AiUsageSummaryResponse>> SummaryAsync(
        DateTimeOffset? from = null, DateTimeOffset? to = null, Guid? tenantId = null, CancellationToken ct = default)
    {
        var result = await api.Ai.Usage.Summary.GetAsync(rc =>
        {
            if (from is not null)     rc.QueryParameters.From = from;
            if (to is not null)       rc.QueryParameters.To = to;
            if (tenantId is not null) rc.QueryParameters.TenantId = tenantId;
        }, ct);
        return result ?? [];
    }
}
