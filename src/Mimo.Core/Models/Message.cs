using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Mensaje individual dentro de una conversación.
/// Puede ser del ciudadano, del bot, de un funcionario o del sistema.
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    /// <summary>Rol del emisor del mensaje.</summary>
    public MessageRole Role { get; set; }

    /// <summary>ID del funcionario si el mensaje fue enviado por un agente humano.</summary>
    public Guid? SenderId { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Metadatos opcionales en JSON: intenciones detectadas, documentos usados como contexto,
    /// herramientas MCP invocadas, etc.
    /// </summary>
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
