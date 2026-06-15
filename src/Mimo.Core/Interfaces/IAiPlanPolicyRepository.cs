using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a las políticas de IA por plan (esquema public, GlobalDbContext). Gestionado por el SuperAdmin.
/// </summary>
public interface IAiPlanPolicyRepository
{
    Task<IReadOnlyList<AiPlanPolicy>> ListAsync(CancellationToken ct = default);
    Task<AiPlanPolicy?> GetByPlanCodeAsync(string planCode, CancellationToken ct = default);
    Task<AiPlanPolicy> AddAsync(AiPlanPolicy policy, CancellationToken ct = default);
    Task UpdateAsync(AiPlanPolicy policy, CancellationToken ct = default);
    Task DeleteAsync(AiPlanPolicy policy, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
