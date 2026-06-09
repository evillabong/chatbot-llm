using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Ticket;

/// <summary>
/// Representación pública de un ticket de atención.
/// </summary>
public record TicketResponse(
    Guid Id,
    Guid ConversationId,
    Guid AssignedRoleId,
    Guid? AssignedAgentId,
    TicketPriority Priority,
    TicketStatus Status,
    string? EscalationReason,
    string? InternalNotes,
    DateTime CreatedAt,
    DateTime? AssignedAt,
    DateTime? FirstResponseAt,
    DateTime? ResolvedAt,
    int QueuePosition
);
