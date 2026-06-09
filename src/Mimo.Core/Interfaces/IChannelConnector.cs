using Mimo.Core.DTOs.Webhook;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Contrato común para todos los conectores de canales externos
/// (Facebook, WhatsApp, Telegram, Instagram, WebChat).
/// </summary>
public interface IChannelConnector
{
    /// <summary>Nombre del canal que implementa este conector.</summary>
    string ChannelName { get; }

    /// <summary>
    /// Normaliza el payload del webhook externo a un mensaje unificado.
    /// Devuelve <see cref="IncomingWebhookMessage"/> con el ExternalUserId y el contenido.
    /// </summary>
    Task<IncomingWebhookMessage> NormalizeIncomingMessageAsync(string rawPayload, CancellationToken ct = default);

    /// <summary>Envía una respuesta al ciudadano a través del canal.</summary>
    Task SendMessageAsync(string externalUserId, string content, CancellationToken ct = default);

    /// <summary>Valida la firma/token del webhook para garantizar autenticidad.</summary>
    bool ValidateSignature(string rawPayload, string signature);
}
