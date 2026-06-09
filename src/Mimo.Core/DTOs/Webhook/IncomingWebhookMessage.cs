namespace Mimo.Core.DTOs.Webhook;

/// <summary>
/// Mensaje normalizado proveniente de cualquier canal externo.
/// Prodcto de <see cref="Mimo.Core.Interfaces.IChannelConnector.NormalizeIncomingMessageAsync"/>.
/// </summary>
public record IncomingWebhookMessage(
    /// <summary>Identificador del ciudadano en el canal de origen (PSID, chat_id, teléfono, etc.).</summary>
    string ExternalUserId,

    /// <summary>Texto del mensaje.</summary>
    string Content,

    /// <summary>Nombre del ciudadano reportado por el canal (opcional).</summary>
    string? ExternalUserName = null
);
