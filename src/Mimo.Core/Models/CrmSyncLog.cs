namespace Mimo.Core.Models;

/// <summary>
/// Bitácora de un intento de sincronización de una oportunidad con un CRM externo (#26). Vive en el
/// esquema del tenant. Registra el proveedor usado, el resultado y el id externo asignado. Aislada por
/// organización.
/// </summary>
public class CrmSyncLog
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OpportunityId { get; set; }

    /// <summary>Proveedor de CRM usado (p. ej. "simulated", "hubspot").</summary>
    public string Provider { get; set; } = string.Empty;

    public bool Success { get; set; }

    /// <summary>Id de la oportunidad en el CRM externo (si la sincronización fue exitosa).</summary>
    public string? ExternalId { get; set; }

    /// <summary>Mensaje del resultado (detalle o error).</summary>
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
