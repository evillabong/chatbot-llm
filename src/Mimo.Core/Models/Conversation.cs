using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Representa el hilo completo de comunicación entre un ciudadano y el sistema
/// (bot o funcionario) a través de cualquier canal.
/// </summary>
public class Conversation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Identificador del ciudadano en el canal de origen (ej: PSID de Facebook, chat_id de Telegram).</summary>
    public string ExternalUserId { get; set; } = string.Empty;

    /// <summary>Nombre del ciudadano reportado por el canal.</summary>
    public string? ExternalUserName { get; set; }

    /// <summary>Canal por el que inició la conversación.</summary>
    public Channel Channel { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.BotActive;

    /// <summary>Indica si el ciudadano se autenticó, dando acceso a documentos privados.</summary>
    public bool IsAuthenticated { get; set; } = false;

    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = [];
    public Ticket? Ticket { get; set; }
}
