namespace Mimo.Core.Models;

/// <summary>
/// Registro de auditoría de cada transferencia de ticket entre roles o funcionarios.
/// </summary>
public class TransferRecord
{
    public Guid Id { get; set; }

    /// <summary>
    /// Conversación a la que pertenece la transferencia. El historial se ancla a la CONVERSACIÓN
    /// (estable) y no al ticket: al transferir se crea un ticket nuevo y el de origen se reemplaza
    /// (cardinalidad conversación↔ticket 1:1), por lo que anclarlo al ticket perdía el historial.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>Id del ticket de ORIGEN al momento de transferir (informativo; puede ya no existir).</summary>
    public Guid TicketId { get; set; }

    public Guid? FromRoleId { get; set; }
    public Guid? FromAgentId { get; set; }
    public Guid ToRoleId { get; set; }
    public Guid? ToAgentId { get; set; }

    /// <summary>Funcionario que ejecutó la transferencia.</summary>
    public Guid TransferredBy { get; set; }

    /// <summary>Motivo declarado de la transferencia.</summary>
    public string? Reason { get; set; }

    /// <summary>Indica si fue una transferencia parcial del historial.</summary>
    public bool IsPartial { get; set; } = false;

    /// <summary>Nota de contexto enviada al rol/funcionario destino.</summary>
    public string? ContextNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
