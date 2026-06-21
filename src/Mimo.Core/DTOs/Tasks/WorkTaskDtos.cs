using System.ComponentModel.DataAnnotations;
using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Tasks;

/// <summary>Tarea operativa (#24).</summary>
public record WorkTaskResponse(
    Guid Id,
    string Title,
    string? Description,
    WorkTaskStatus Status,
    Guid? AssignedAgentId,
    DateTime? DueAt,
    Guid? ConversationId,
    Guid? TicketId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt
);

/// <summary>Crea una tarea operativa.</summary>
public record CreateWorkTaskRequest(
    [Required, MaxLength(200)] string Title,
    string? Description = null,
    Guid? AssignedAgentId = null,
    DateTime? DueAt = null,
    Guid? ConversationId = null,
    Guid? TicketId = null
);

/// <summary>Actualiza una tarea operativa (incluye cambio de estado).</summary>
public record UpdateWorkTaskRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    WorkTaskStatus Status,
    Guid? AssignedAgentId,
    DateTime? DueAt
);
