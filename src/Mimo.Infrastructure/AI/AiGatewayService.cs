using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mimo.Core.Enums;
using Mimo.Core.Exceptions;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Models.Ai;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.AI;

/// <summary>
/// Gateway de IA in-process (ver ADR 0005). Punto único por el que pasan todas las
/// consultas al LLM: resuelve el conector activo, valida el entitlement y la cuota del
/// plan del tenant, ejecuta la llamada y registra el uso.
///
/// Opera sobre el esquema public (GlobalDbContext): conectores, políticas de plan,
/// registros de uso y catálogo de tenants viven todos a nivel de plataforma.
/// </summary>
public class AiGatewayService(
    GlobalDbContext db,
    ILlmClientFactory factory,
    ILogger<AiGatewayService> logger) : IAiGatewayService
{
    public async Task<string> ChatAsync(
        Guid tenantId,
        string systemPrompt,
        IReadOnlyList<(string role, string content)> history,
        CancellationToken ct = default)
    {
        var connector = await AuthorizeAsync(tenantId, ct);
        var client    = factory.Create(connector);

        var result = await client.ChatAsync(systemPrompt, history, ct);
        await RecordUsageAsync(tenantId, connector, AiOperation.Chat, connector.Settings.ChatModel, result.Usage, ct);

        return result.Content;
    }

    public async Task<float[]> GetEmbeddingAsync(Guid tenantId, string text, CancellationToken ct = default)
    {
        var connector = await AuthorizeAsync(tenantId, ct);
        var client    = factory.Create(connector);

        var result = await client.GetEmbeddingAsync(text, ct);
        await RecordUsageAsync(tenantId, connector, AiOperation.Embedding, connector.Settings.EmbeddingModel, result.Usage, ct);

        return result.Embedding;
    }

    /// <summary>
    /// Resuelve el conector activo y valida que el plan del tenant permita su uso y no haya
    /// superado la cuota del periodo. Devuelve el conector a usar.
    /// </summary>
    private async Task<AiConnector> AuthorizeAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} no encontrado.");

        var connector = await db.AiConnectors.AsNoTracking().FirstOrDefaultAsync(c => c.IsActive, ct)
            ?? throw new InvalidOperationException("No hay un conector de IA activo configurado.");

        var policy = await db.AiPlanPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanCode == tenant.Plan && p.IsActive, ct);

        // Sin política => permisivo (no romper tenants existentes); ver ADR 0005.
        if (policy is null)
        {
            logger.LogDebug(
                "Plan '{Plan}' del tenant {TenantId} sin política de IA; acceso permisivo.", tenant.Plan, tenantId);
            return connector;
        }

        // Entitlement de modelo/proveedor.
        if (policy.AllowedProviders.Count > 0 && !policy.AllowedProviders.Contains(connector.Provider))
            throw new AiAccessDeniedException(
                $"El plan '{tenant.Plan}' no permite el proveedor de IA '{connector.Provider}'.");

        // Cuota del periodo.
        await EnforceQuotaAsync(tenantId, tenant.Plan, policy, ct);

        return connector;
    }

    private async Task EnforceQuotaAsync(Guid tenantId, string planCode, AiPlanPolicy policy, CancellationToken ct)
    {
        if (policy.MonthlyRequestQuota <= 0 && policy.MonthlyTokenQuota <= 0)
            return;

        var now         = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usage = db.AiUsageRecords.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.CreatedAt >= periodStart);

        if (policy.MonthlyRequestQuota > 0)
        {
            var requests = await usage.CountAsync(ct);
            if (requests >= policy.MonthlyRequestQuota)
                throw new AiQuotaExceededException(
                    $"Cuota mensual de solicitudes de IA alcanzada para el plan '{planCode}' ({policy.MonthlyRequestQuota}).");
        }

        if (policy.MonthlyTokenQuota > 0)
        {
            var tokens = await usage.SumAsync(u => (long)u.TotalTokens, ct);
            if (tokens >= policy.MonthlyTokenQuota)
                throw new AiQuotaExceededException(
                    $"Cuota mensual de tokens de IA alcanzada para el plan '{planCode}' ({policy.MonthlyTokenQuota}).");
        }
    }

    private async Task RecordUsageAsync(
        Guid tenantId, AiConnector connector, AiOperation operation, string model, LlmUsage usage, CancellationToken ct)
    {
        db.AiUsageRecords.Add(new AiUsageRecord
        {
            Id               = Guid.NewGuid(),
            TenantId         = tenantId,
            Provider         = connector.Provider,
            Model            = model,
            Operation        = operation,
            PromptTokens     = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            TotalTokens      = usage.TotalTokens,
            CreatedAt        = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
