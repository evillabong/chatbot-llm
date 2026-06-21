using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de la bitácora de sincronización con CRM externo (#26) dentro del esquema de un tenant.
/// </summary>
public class CrmSyncLogRepository(TenantDbContext db) : ICrmSyncLogRepository
{
    public async Task AddAsync(CrmSyncLog log, CancellationToken ct = default)
    {
        db.CrmSyncLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CrmSyncLog>> ListByOpportunityAsync(
        Guid opportunityId, int limit = 50, CancellationToken ct = default)
        => await db.CrmSyncLogs
            .AsNoTracking()
            .Where(l => l.OpportunityId == opportunityId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
}
