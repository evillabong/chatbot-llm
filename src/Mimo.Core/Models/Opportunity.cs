using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Oportunidad de venta (#26, capacidad opcional de ventas/CRM). Vive en el esquema del tenant. Captura
/// un lead con su contacto, etapa del pipeline, valor estimado y, opcionalmente, la conversación de
/// origen y un responsable. Aislada por organización.
/// </summary>
public class Opportunity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.New;

    /// <summary>Valor estimado (0 = sin monto).</summary>
    public decimal Amount { get; set; }

    /// <summary>Conversación de origen (opcional).</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>Responsable (funcionario) de la oportunidad (opcional).</summary>
    public Guid? AssignedAgentId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Fecha de cierre (al pasar a Won/Lost); null si sigue abierta.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Id de la oportunidad en el CRM externo (#26); null si nunca se sincronizó.</summary>
    public string? ExternalCrmId { get; set; }

    /// <summary>Última sincronización exitosa con el CRM externo; null si nunca.</summary>
    public DateTime? LastSyncedAt { get; set; }
}
