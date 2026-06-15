using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models.Ai;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Consulta agregada del uso de IA (esquema public, GlobalDbContext).
/// </summary>
public class AiUsageRepository(GlobalDbContext db) : IAiUsageRepository
{
    public async Task<IReadOnlyList<AiUsageSummary>> GetSummaryAsync(
        DateTime fromUtc, DateTime toUtc, Guid? tenantId, CancellationToken ct = default)
    {
        var query = db.AiUsageRecords.AsNoTracking()
            .Where(u => u.CreatedAt >= fromUtc && u.CreatedAt < toUtc);

        if (tenantId.HasValue)
            query = query.Where(u => u.TenantId == tenantId.Value);

        // Agregación en la BD; el ordenamiento final se hace en memoria para mantener la
        // consulta traducible por distintos proveedores. Las sumas (int) se ensanchan a long.
        var grouped = await query
            .GroupBy(u => u.TenantId)
            .Select(g => new
            {
                TenantId   = g.Key,
                Requests   = g.Count(),
                Prompt     = g.Sum(u => u.PromptTokens),
                Completion = g.Sum(u => u.CompletionTokens),
                Total      = g.Sum(u => u.TotalTokens)
            })
            .ToListAsync(ct);

        return grouped
            .Select(g => new AiUsageSummary(g.TenantId, g.Requests, g.Prompt, g.Completion, g.Total))
            .OrderByDescending(s => s.TotalTokens)
            .ToList();
    }
}
