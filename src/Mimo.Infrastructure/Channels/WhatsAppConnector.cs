using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Mimo.Core.DTOs.Webhook;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Channels;

/// <summary>
/// Conector para WhatsApp Business API (Meta Cloud API) — stub.
/// Firma: X-Hub-Signature-256 (mismo mecanismo que Facebook).
/// </summary>
public class WhatsAppConnector(ILogger<WhatsAppConnector> logger) : IChannelConnector
{
    public string ChannelName => "whatsapp";

    public Task<IncomingWebhookMessage> NormalizeIncomingMessageAsync(string rawPayload, CancellationToken ct = default)
    {
        logger.LogWarning("WhatsAppConnector: normalización no implementada");
        throw new NotSupportedException("WhatsAppConnector no implementado todavía.");
    }

    public Task SendMessageAsync(string externalUserId, string content, CancellationToken ct = default)
    {
        // TODO: POST a https://graph.facebook.com/v18.0/{phoneNumberId}/messages
        logger.LogWarning("WhatsAppConnector: envío no implementado");
        throw new NotSupportedException("WhatsAppConnector no implementado todavía.");
    }

    public bool ValidateSignature(string rawPayload, string signature)
    {
        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        var expected  = signature["sha256=".Length..];
        var appSecret = Environment.GetEnvironmentVariable("WHATSAPP_APP_SECRET") ?? string.Empty;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash       = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload)));

        return hash.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
