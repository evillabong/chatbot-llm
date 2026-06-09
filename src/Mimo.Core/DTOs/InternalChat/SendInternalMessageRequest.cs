namespace Mimo.Core.DTOs.InternalChat;

/// <summary>Solicitud para enviar un mensaje de chat interno entre funcionarios.</summary>
public record SendInternalMessageRequest(
    Guid ToAgentId,
    string Content,
    Guid? RelatedTicketId
);
