using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso al catálogo de planes (esquema public, GlobalDbContext).
/// </summary>
public interface IPlanRepository
{
    Task<bool> ExistsAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Plan>> ListAsync(bool? isActive = null, CancellationToken ct = default);
    Task<Plan?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Plan> AddAsync(Plan plan, CancellationToken ct = default);
    Task UpdateAsync(Plan plan, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
