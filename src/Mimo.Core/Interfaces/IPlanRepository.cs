using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso al catálogo de planes (esquema public, GlobalDbContext).
/// </summary>
public interface IPlanRepository
{
    Task<bool> ExistsAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Plan>> ListAsync(bool? isActive = null, CancellationToken ct = default);
}
