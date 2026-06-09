using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Mimo.Core.DTOs.Webhook;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Channels;

/// <summary>
/// Conector para Instagram Messaging (Meta Graph API) — stub.
/// Comparte el mecanismo de firma HMAC-SHA256 con Facebook.
/// </summary>
public class InstagramConnector(ILogger<InstagramConnector> logger) : IChannelConnector
{
    public string ChannelName => "instagram";

    public Task<IncomingWebhookMessage> NormalizeIncomingMessageAsync(string rawPayload, CancellationToken ct = default)
    {
        logger.LogWarning("InstagramConnector: normalización no implementada");
        throw new NotSupportedException("InstagramConnector no implementado todavía.");
    }

    public Task SendMessageAsync(string externalUserId, string content, CancellationToken ct = default)
    {
        // TODO: POST a https://graph.facebook.com/v18.0/{ig-user-id}/messages
        logger.LogWarning("InstagramConnector: envío no implementado");
        throw new NotSupportedException("InstagramConnector no implementado todavía.");
    }

    public bool ValidateSignature(string rawPayload, string signature)
    {
        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        var expected  = signature["sha256=".Length..];
        var appSecret = Environment.GetEnvironmentVariable("INSTAGRAM_APP_SECRET") ?? string.Empty;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash       = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload)));

        return hash.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
