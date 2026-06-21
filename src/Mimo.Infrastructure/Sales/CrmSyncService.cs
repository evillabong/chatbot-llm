using Microsoft.Extensions.Logging;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Sales;

namespace Mimo.Infrastructure.Sales;

/// <summary>
/// Orquesta la sincronización de una oportunidad con el CRM externo (#26): invoca al proveedor (hoy
/// simulado), actualiza el estado de sync en la oportunidad y registra la bitácora. No propaga
/// excepciones del proveedor: devuelve un resultado fallido y lo deja en la bitácora.
/// </summary>
public sealed class CrmSyncService(
    IOpportunityRepository opportunities,
    ICrmSyncProvider provider,
    ICrmSyncLogRepository logs,
    ILogger<CrmSyncService> logger) : ICrmSyncService
{
    public async Task<CrmSyncResult> SyncOpportunityAsync(Guid opportunityId, CancellationToken ct = default)
    {
        var opportunity = await opportunities.GetByIdAsync(opportunityId, ct);
        if (opportunity is null)
            return new CrmSyncResult(false, null, "Oportunidad no encontrada.");

        CrmSyncResult result;
        try
        {
            result = await provider.SyncOpportunityAsync(opportunity, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo al sincronizar la oportunidad {OpportunityId} con el CRM.", opportunityId);
            result = new CrmSyncResult(false, null, $"Error del proveedor: {ex.Message}");
        }

        if (result.Success)
        {
            opportunity.ExternalCrmId = result.ExternalId;
            opportunity.LastSyncedAt  = DateTime.UtcNow;
            await opportunities.SaveChangesAsync(ct);
        }

        await logs.AddAsync(new CrmSyncLog
        {
            Id            = Guid.NewGuid(),
            TenantId      = opportunity.TenantId,
            OpportunityId = opportunity.Id,
            Provider      = provider.Name,
            Success       = result.Success,
            ExternalId    = result.ExternalId,
            Message       = result.Message,
            CreatedAt     = DateTime.UtcNow
        }, ct);

        return result;
    }
}
