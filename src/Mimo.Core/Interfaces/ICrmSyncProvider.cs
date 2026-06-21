using Mimo.Core.Models;
using Mimo.Core.Sales;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Proveedor de sincronización con un CRM externo (#26). La implementación real (HubSpot, Salesforce…)
/// hablaría con la API del proveedor; el proveedor por defecto es **simulado** para no bloquear la
/// funcionalidad hasta tener credenciales reales. Aislado por organización (el CRM es del tenant).
/// </summary>
public interface ICrmSyncProvider
{
    /// <summary>Nombre del proveedor (p. ej. "simulated"), para auditoría.</summary>
    string Name { get; }

    /// <summary>Crea/actualiza la oportunidad en el CRM externo y devuelve su id externo.</summary>
    Task<CrmSyncResult> SyncOpportunityAsync(Opportunity opportunity, CancellationToken ct = default);
}
