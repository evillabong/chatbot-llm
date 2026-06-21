using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de sugerencias de conocimiento (#22) dentro del esquema de un tenant.
/// </summary>
public class KnowledgeSuggestionRepository(TenantDbContext db) : IKnowledgeSuggestionRepository
{
    public Task<KnowledgeSuggestion?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.KnowledgeSuggestions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<KnowledgeSuggestion>> ListAsync(
        KnowledgeSuggestionStatus? status = null, CancellationToken ct = default)
    {
        var query = db.KnowledgeSuggestions.AsNoTracking().AsQueryable();
        if (status is not null)
            query = query.Where(s => s.Status == status.Value);
        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
    }

    public async Task<KnowledgeSuggestion> AddAsync(KnowledgeSuggestion suggestion, CancellationToken ct = default)
    {
        db.KnowledgeSuggestions.Add(suggestion);
        await db.SaveChangesAsync(ct);
        return suggestion;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
