using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de oportunidades de venta (#26) dentro del esquema de un tenant.
/// </summary>
public class OpportunityRepository(TenantDbContext db) : IOpportunityRepository
{
    public Task<Opportunity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Opportunities.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Opportunity>> ListAsync(
        OpportunityStage? stage = null, Guid? assignedAgentId = null, CancellationToken ct = default)
    {
        var query = db.Opportunities.AsNoTracking().AsQueryable();
        if (stage is not null)
            query = query.Where(o => o.Stage == stage.Value);
        if (assignedAgentId is not null)
            query = query.Where(o => o.AssignedAgentId == assignedAgentId.Value);
        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
    }

    public async Task<Opportunity> AddAsync(Opportunity opportunity, CancellationToken ct = default)
    {
        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync(ct);
        return opportunity;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task RemoveAsync(Opportunity opportunity, CancellationToken ct = default)
    {
        db.Opportunities.Remove(opportunity);
        await db.SaveChangesAsync(ct);
    }
}
