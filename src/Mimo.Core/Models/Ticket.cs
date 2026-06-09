using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Ticket de atención humana asociado a una conversación.
/// Se crea cuando el ciudadano solicita atención o el sistema lo determina necesario.
/// </summary>
public class Ticket
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    /// <summary>Rol responsable de atender este ticket.</summary>
    public Guid AssignedRoleId { get; set; }

    /// <summary>Funcionario asignado. Null si aún está en cola.</summary>
    public Guid? AssignedAgentId { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.InQueue;

    /// <summary>Notas internas del funcionario, no visibles al ciudadano.</summary>
    public string? InternalNotes { get; set; }

    /// <summary>Motivo por el que se escaló a atención humana.</summary>
    public string? EscalationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public ICollection<TransferRecord> TransferRecords { get; set; } = [];
    public SatisfactionSurvey? Survey { get; set; }
}
