using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio del catálogo de planes (esquema public, GlobalDbContext).
/// </summary>
public class PlanRepository(GlobalDbContext db) : IPlanRepository
{
    public Task<bool> ExistsAsync(string code, CancellationToken ct = default)
    {
        var normalized = Plan.NormalizeCode(code);
        return db.Plans.AnyAsync(p => p.Code == normalized && p.IsActive, ct);
    }

    public async Task<IReadOnlyList<Plan>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var query = db.Plans.AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query.OrderBy(p => p.Code).ToListAsync(ct);
    }
}
