using Microsoft.Extensions.Logging;
using Mimo.Core.DTOs.Webhook;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Channels;

/// <summary>
/// Conector para Telegram Bot API — stub.
/// Firma: X-Telegram-Bot-Api-Secret-Token (header).
/// </summary>
public class TelegramConnector(ILogger<TelegramConnector> logger) : IChannelConnector
{
    public string ChannelName => "telegram";

    public Task<IncomingWebhookMessage> NormalizeIncomingMessageAsync(string rawPayload, CancellationToken ct = default)
    {
        logger.LogWarning("TelegramConnector: normalización no implementada");
        throw new NotSupportedException("TelegramConnector no implementado todavía.");
    }

    public Task SendMessageAsync(string externalUserId, string content, CancellationToken ct = default)
    {
        // TODO: POST a https://api.telegram.org/bot{token}/sendMessage
        logger.LogWarning("TelegramConnector: envío no implementado");
        throw new NotSupportedException("TelegramConnector no implementado todavía.");
    }

    public bool ValidateSignature(string rawPayload, string signature)
    {
        // Telegram usa X-Telegram-Bot-Api-Secret-Token (token secreto fijo)
        var expected = Environment.GetEnvironmentVariable("TELEGRAM_WEBHOOK_SECRET") ?? string.Empty;
        return expected.Length > 0 && signature == expected;
    }
}
