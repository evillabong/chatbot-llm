using System.ComponentModel.DataAnnotations;
using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Conversation;

/// <summary>
/// Solicitud para iniciar una nueva conversación desde el WebChat.
/// </summary>
public record StartConversationRequest(
    [Required] string ExternalUserId,
    string? ExternalUserName = null,
    Channel Channel = Channel.WebChat
);
