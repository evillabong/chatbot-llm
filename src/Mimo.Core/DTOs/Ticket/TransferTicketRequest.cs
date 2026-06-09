namespace Mimo.Core.DTOs.Ticket;

/// <summary>
/// Solicitud de transferencia de un ticket a otro rol o funcionario.
/// </summary>
public record TransferTicketRequest(
    Guid ToRoleId,
    Guid? ToAgentId,
    string? Reason,
    string? ContextNote,
    bool IsPartial
);
