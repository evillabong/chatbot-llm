using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a la bitácora de sincronización con CRM externo (#26) dentro del esquema de un tenant.
/// </summary>
public interface ICrmSyncLogRepository
{
    Task AddAsync(CrmSyncLog log, CancellationToken ct = default);
    Task<IReadOnlyList<CrmSyncLog>> ListByOpportunityAsync(Guid opportunityId, int limit = 50, CancellationToken ct = default);
}
