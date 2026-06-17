namespace Mimo.Core.DTOs.Ticket;

/// <summary>Resultado de transferir un ticket: el nuevo ticket creado en el rol destino.</summary>
public record TransferTicketResponse(
    Guid NewTicketId,
    string Message
);
