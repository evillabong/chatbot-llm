using System.Data;
using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de conectores de IA (esquema public, GlobalDbContext).
/// </summary>
public class AiConnectorRepository(GlobalDbContext db) : IAiConnectorRepository
{
    public async Task<IReadOnlyList<AiConnector>> ListAsync(CancellationToken ct = default)
        => await db.AiConnectors.AsNoTracking().OrderBy(c => c.Provider).ToListAsync(ct);

    public Task<AiConnector?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.AiConnectors.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ProviderExistsAsync(string provider, CancellationToken ct = default)
        => db.AiConnectors.AnyAsync(c => c.Provider == provider, ct);

    public async Task<AiConnector> AddAsync(AiConnector connector, CancellationToken ct = default)
    {
        db.AiConnectors.Add(connector);
        await db.SaveChangesAsync(ct);
        return connector;
    }

    public Task UpdateAsync(AiConnector connector, CancellationToken ct = default)
    {
        db.AiConnectors.Update(connector);
        return Task.CompletedTask;
    }

    public async Task<bool> SetActiveAsync(Guid id, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var target = await db.AiConnectors.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (target is null)
            {
                await tx.CommitAsync(ct);
                return false;
            }

            // Solo un conector activo a la vez: desactivar el resto y activar el indicado.
            await db.AiConnectors
                .Where(c => c.Id != id && c.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);

            target.IsActive  = true;
            target.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return true;
        });
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
