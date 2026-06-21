using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a las oportunidades de venta (#26) dentro del esquema de un tenant.
/// </summary>
public interface IOpportunityRepository
{
    Task<Opportunity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Opportunity>> ListAsync(OpportunityStage? stage = null, Guid? assignedAgentId = null, CancellationToken ct = default);
    Task<Opportunity> AddAsync(Opportunity opportunity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task RemoveAsync(Opportunity opportunity, CancellationToken ct = default);
}
