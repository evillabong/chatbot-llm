using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de políticas de IA por plan (esquema public, GlobalDbContext).
/// </summary>
public class AiPlanPolicyRepository(GlobalDbContext db) : IAiPlanPolicyRepository
{
    public async Task<IReadOnlyList<AiPlanPolicy>> ListAsync(CancellationToken ct = default)
        => await db.AiPlanPolicies.AsNoTracking().OrderBy(p => p.PlanCode).ToListAsync(ct);

    public Task<AiPlanPolicy?> GetByPlanCodeAsync(string planCode, CancellationToken ct = default)
    {
        var normalized = Plan.NormalizeCode(planCode);
        return db.AiPlanPolicies.FirstOrDefaultAsync(p => p.PlanCode == normalized, ct);
    }

    public async Task<AiPlanPolicy> AddAsync(AiPlanPolicy policy, CancellationToken ct = default)
    {
        db.AiPlanPolicies.Add(policy);
        await db.SaveChangesAsync(ct);
        return policy;
    }

    public Task UpdateAsync(AiPlanPolicy policy, CancellationToken ct = default)
    {
        db.AiPlanPolicies.Update(policy);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AiPlanPolicy policy, CancellationToken ct = default)
    {
        db.AiPlanPolicies.Remove(policy);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
