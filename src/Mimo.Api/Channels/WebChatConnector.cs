using Microsoft.AspNetCore.SignalR;
using Mimo.Api.Hubs;
using Mimo.Core.DTOs.Conversation;
using Mimo.Core.DTOs.Webhook;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Channels;

/// <summary>
/// Conector del canal WebChat integrado (iframe / widget JS).
/// Las respuestas se envían en tiempo real a través del ChatHub (SignalR).
/// No requiere validación de firma al ser un canal de primera parte.
/// </summary>
public class WebChatConnector(
    IHubContext<ChatHub> chatHub,
    IConversationRepository conversations) : IChannelConnector
{
    public string ChannelName => "webchat";

    /// <summary>
    /// Normaliza el payload del WebChat.
    /// El payload es JSON: {"externalUserId":"...","content":"...","externalUserName":"..."}
    /// </summary>
    public Task<IncomingWebhookMessage> NormalizeIncomingMessageAsync(string rawPayload, CancellationToken ct = default)
    {
        var parts = System.Text.Json.JsonSerializer.Deserialize<WebChatPayload>(rawPayload)!;
        return Task.FromResult(new IncomingWebhookMessage(parts.ExternalUserId, parts.Content, parts.ExternalUserName));
    }

    /// <summary>
    /// Envía la respuesta del bot al ciudadano a través del grupo de la conversación en ChatHub.
    /// </summary>
    public async Task SendMessageAsync(string externalUserId, string content, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByExternalUserAsync(externalUserId, Channel.WebChat, ct);
        if (conversation is null) return;

        var msgDto = new MessageResponse(Guid.NewGuid(), MessageRole.Assistant, content, DateTime.UtcNow);

        await chatHub.Clients
            .Group(conversation.Id.ToString())
            .SendAsync("MessageReceived", msgDto, ct);
    }

    /// <summary>
    /// El WebChat es un canal de primera parte; no requiere validación de firma.
    /// </summary>
    public bool ValidateSignature(string rawPayload, string signature) => true;

    private record WebChatPayload(string ExternalUserId, string Content, string? ExternalUserName = null);
}
