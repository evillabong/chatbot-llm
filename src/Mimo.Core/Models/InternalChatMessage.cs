namespace Mimo.Core.Models;

/// <summary>
/// Mensaje de chat interno entre funcionarios del mismo tenant.
/// Puede estar asociado a un ticket específico o ser una conversación general.
/// </summary>
public class InternalChatMessage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid FromAgentId { get; set; }
    public Guid ToAgentId { get; set; }

    /// <summary>Ticket sobre el que trata el mensaje. Null si es conversación general.</summary>
    public Guid? RelatedTicketId { get; set; }

    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
