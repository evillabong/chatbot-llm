using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Conversation;

/// <summary>
/// Estado y mensajes recientes de una conversación.
/// </summary>
public record ConversationResponse(
    Guid Id,
    Channel Channel,
    TicketStatus Status,
    bool IsAuthenticated,
    DateTime CreatedAt,
    IReadOnlyList<MessageResponse> Messages
);
