using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Mimo.Core.DTOs.Webhook;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Channels;

/// <summary>
/// Conector para Facebook Messenger (stub).
/// Valida la firma HMAC-SHA256 del webhook de Meta.
/// El envío de mensajes requiere configurar las credenciales en TenantConfiguration.
/// </summary>
public class FacebookConnector(ILogger<FacebookConnector> logger) : IChannelConnector
{
    public string ChannelName => "facebook";

    public Task<IncomingWebhookMessage> NormalizeIncomingMessageAsync(string rawPayload, CancellationToken ct = default)
    {
        // TODO: deserializar el payload de Messenger y extraer sender + text
        logger.LogWarning("FacebookConnector: normalización no implementada");
        throw new NotSupportedException("FacebookConnector no implementado todavía.");
    }

    public Task SendMessageAsync(string externalUserId, string content, CancellationToken ct = default)
    {
        // TODO: POST a https://graph.facebook.com/v18.0/me/messages con token de la página
        logger.LogWarning("FacebookConnector: envío no implementado");
        throw new NotSupportedException("FacebookConnector no implementado todavía.");
    }

    public bool ValidateSignature(string rawPayload, string signature)
    {
        // Meta envía X-Hub-Signature-256: sha256=<hash>
        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        var expected  = signature["sha256=".Length..];
        var appSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET") ?? string.Empty;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash       = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload)));

        return hash.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
