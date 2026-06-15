using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Ai;
using Mimo.Core.Interfaces;

namespace Mimo.Admin.Api.Endpoints;

/// <summary>
/// Estadísticas de uso de IA por tenant. Solo SuperAdmin.
/// </summary>
public static class AiUsageEndpoints
{
    public static IEndpointRouteBuilder MapAiUsageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ai/usage")
            .WithTags("AiUsage")
            .RequireAuthorization(MimoAuthorization.Policies.SuperAdmin);

        group.MapGet("/summary", SummaryAsync)
            .WithName("GetAiUsageSummary")
            .WithSummary("Resume el uso de IA por tenant en un periodo (query: from, to, tenantId; por defecto el mes actual).");

        return app;
    }

    private static async Task<IResult> SummaryAsync(
        IAiUsageRepository repo,
        DateTime? from = null,
        DateTime? to = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var now      = DateTime.UtcNow;
        var fromUtc  = (from ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)).ToUniversalTime();
        var toUtc    = (to ?? now).ToUniversalTime();

        if (toUtc <= fromUtc)
            return Results.BadRequest(new { error = "El parámetro 'to' debe ser posterior a 'from'." });

        var summary = await repo.GetSummaryAsync(fromUtc, toUtc, tenantId, ct);

        var response = summary
            .Select(s => new AiUsageSummaryResponse(
                s.TenantId, s.Requests, s.PromptTokens, s.CompletionTokens, s.TotalTokens))
            .ToList();

        return Results.Ok(response);
    }
}
