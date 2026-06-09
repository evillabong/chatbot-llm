using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Conversation;

/// <summary>
/// Mensaje enviado por el ciudadano al bot o funcionario.
/// </summary>
public record SendMessageRequest(
    [Required, MaxLength(4000)] string Content
);
