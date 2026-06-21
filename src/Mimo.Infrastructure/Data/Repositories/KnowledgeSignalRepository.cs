using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de señales de recuperación semántica (#22) dentro del esquema de un tenant.
/// </summary>
public class KnowledgeSignalRepository(TenantDbContext db) : IKnowledgeSignalRepository
{
    public async Task AddAsync(KnowledgeQuerySignal signal, CancellationToken ct = default)
    {
        db.KnowledgeQuerySignals.Add(signal);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<KnowledgeQuerySignal>> GetRecentGapsAsync(
        int limit = 50, CancellationToken ct = default)
        => await db.KnowledgeQuerySignals
            .AsNoTracking()
            .Where(s => s.KnowledgeGap)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
}
