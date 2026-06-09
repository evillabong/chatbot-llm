using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Conversation;

/// <summary>
/// Representación de un mensaje para el cliente.
/// </summary>
public record MessageResponse(
    Guid Id,
    MessageRole Role,
    string Content,
    DateTime CreatedAt
);
