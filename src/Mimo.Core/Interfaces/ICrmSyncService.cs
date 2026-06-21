using Mimo.Core.Sales;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Orquesta la sincronización de una oportunidad con el CRM externo (#26): invoca al proveedor,
/// actualiza el estado de sync en la oportunidad y registra la bitácora. Best-effort en disparos
/// automáticos (no debe romper la operación origen).
/// </summary>
public interface ICrmSyncService
{
    Task<CrmSyncResult> SyncOpportunityAsync(Guid opportunityId, CancellationToken ct = default);
}
