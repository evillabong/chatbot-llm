using Mimo.Core.Common;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a datos de funcionarios dentro del esquema de un tenant.
/// </summary>
public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Agent>> ListAsync(bool? isActive = null, CancellationToken ct = default);
    Task<PagedResult<Agent>> ListPagedAsync(bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Agent>> GetByRoleAsync(Guid roleId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<Agent> AddAsync(Agent agent, CancellationToken ct = default);
    Task UpdateAsync(Agent agent, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
