using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Tarea operativa (#24, automatización). Vive en el esquema del tenant. Entidad con responsable,
/// vencimiento y estado, vinculable opcionalmente a una conversación o ticket. Es la base sobre la que
/// el motor de reglas (corte posterior) podrá crear tareas como acción. Aislada por organización.
/// </summary>
public class WorkTask
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Pending;

    /// <summary>Funcionario responsable (opcional).</summary>
    public Guid? AssignedAgentId { get; set; }

    /// <summary>Fecha de vencimiento (opcional).</summary>
    public DateTime? DueAt { get; set; }

    /// <summary>Conversación vinculada (opcional).</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>Ticket vinculado (opcional).</summary>
    public Guid? TicketId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Fecha en que pasó a un estado terminal (Done/Cancelled); null si sigue abierta.</summary>
    public DateTime? CompletedAt { get; set; }
}
