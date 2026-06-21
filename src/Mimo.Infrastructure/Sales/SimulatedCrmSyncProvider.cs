using Microsoft.Extensions.Logging;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Sales;

namespace Mimo.Infrastructure.Sales;

/// <summary>
/// Proveedor de CRM **simulado** (#26): no llama a ningún sistema externo; asigna un id externo
/// determinista por oportunidad y simula éxito. Sirve para ejercitar la sincronización de extremo a
/// extremo (estado + bitácora + disparos) hasta enchufar un proveedor real con la misma interfaz.
/// </summary>
public sealed class SimulatedCrmSyncProvider(ILogger<SimulatedCrmSyncProvider> logger) : ICrmSyncProvider
{
    public string Name => "simulated";

    public Task<CrmSyncResult> SyncOpportunityAsync(Opportunity opportunity, CancellationToken ct = default)
    {
        // Id externo estable y reproducible: conserva el existente o deriva uno del id de la oportunidad.
        var externalId = string.IsNullOrEmpty(opportunity.ExternalCrmId)
            ? $"SIM-{opportunity.Id.ToString("N")[..12].ToUpperInvariant()}"
            : opportunity.ExternalCrmId;

        logger.LogInformation(
            "CRM simulado: sincronizada oportunidad {OpportunityId} → {ExternalId}", opportunity.Id, externalId);

        return Task.FromResult(new CrmSyncResult(true, externalId, "Sincronización simulada correcta."));
    }
}
