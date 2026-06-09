namespace Mimo.Core.DTOs.InternalChat;

/// <summary>Representación de un mensaje de chat interno.</summary>
public record InternalMessageResponse(
    Guid Id,
    Guid FromAgentId,
    Guid ToAgentId,
    Guid? RelatedTicketId,
    string Content,
    bool IsRead,
    DateTime CreatedAt
);
