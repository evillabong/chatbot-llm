using Microsoft.AspNetCore.Mvc;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Pipeline de entrada de webhooks para canales externos.
/// Todos los canales confluyen aquí y se procesan de forma uniforme.
/// </summary>
public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webhooks")
            .WithTags("Webhooks");

        // POST /webhooks/{channel}/incoming
        group.MapPost("/{channel}/incoming", HandleIncomingAsync)
            .WithName("WebhookIncoming")
            .WithSummary("Recibe y procesa mensajes entrantes de un canal externo.");

        // GET /webhooks/{channel}/incoming — verificación de webhook (Meta: Facebook/WhatsApp/Instagram)
        group.MapGet("/{channel}/incoming", VerifyWebhookAsync)
            .WithName("WebhookVerify")
            .WithSummary("Verificación del endpoint de webhook para Meta (Facebook/WhatsApp/Instagram).");

        return app;
    }

    /// <summary>
    /// Recibe el payload del webhook, valida la firma, normaliza el mensaje
    /// y lo enruta a través del orquestador.
    /// </summary>
    private static async Task<IResult> HandleIncomingAsync(
        string channel,
        HttpRequest request,
        HttpContext context,
        IKeyedServiceProvider services,
        IConversationOrchestrator orchestrator,
        IConversationRepository repo,
        TenantDbContext db,
        CancellationToken ct)
    {
        // Resolver el conector por nombre de canal (keyed DI)
        var connector = services.GetKeyedService<IChannelConnector>(channel.ToLowerInvariant());
        if (connector is null)
            return Results.NotFound($"Canal '{channel}' no está soportado.");

        // Leer body como string para validación de firma y normalización
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync(ct);
        request.Body.Position = 0;

        // Validar firma del webhook
        var signature = request.Headers["X-Hub-Signature-256"].ToString();
        if (string.IsNullOrEmpty(signature))
            signature = request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();

        if (!connector.ValidateSignature(rawPayload, signature))
            return Results.Unauthorized();

        // Normalizar payload
        Core.DTOs.Webhook.IncomingWebhookMessage incoming;
        try
        {
            incoming = await connector.NormalizeIncomingMessageAsync(rawPayload, ct);
        }
        catch (NotSupportedException)
        {
            // Conector stub: aceptar silenciosamente para no generar alertas en Meta
            return Results.Ok();
        }

        // Resolver el canal enum
        if (!Enum.TryParse<Channel>(channel, ignoreCase: true, out var channelEnum))
            return Results.BadRequest("Canal no reconocido.");

        var tenantId = (Guid)context.Items["TenantId"]!;

        // Obtener o crear la conversación para este usuario externo
        var conversation = await repo.GetByExternalUserAsync(incoming.ExternalUserId, channelEnum, ct);
        if (conversation is null)
        {
            conversation = new Conversation
            {
                Id               = Guid.NewGuid(),
                TenantId         = tenantId,
                ExternalUserId   = incoming.ExternalUserId,
                ExternalUserName = incoming.ExternalUserName,
                Channel          = channelEnum,
                Status           = TicketStatus.BotActive,
                CreatedAt        = DateTime.UtcNow,
                LastMessageAt    = DateTime.UtcNow
            };
            await repo.AddAsync(conversation, ct);
        }

        // Procesar el mensaje a través del orquestador
        var responseMessage = await orchestrator.HandleIncomingMessageAsync(conversation.Id, incoming.Content, ct);

        // Enviar respuesta al ciudadano a través del canal
        if (!string.IsNullOrWhiteSpace(responseMessage?.Content))
            await connector.SendMessageAsync(incoming.ExternalUserId, responseMessage.Content, ct);

        return Results.Ok();
    }

    /// <summary>
    /// Verificación de suscripción de webhook de Meta (Facebook/Instagram/WhatsApp).
    /// Meta envía hub.challenge que debe devolverse en el cuerpo.
    /// </summary>
    private static IResult VerifyWebhookAsync(
        string channel,
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        IConfiguration configuration)
    {
        if (mode != "subscribe" || challenge is null)
            return Results.BadRequest();

        var expectedToken = configuration[$"Webhooks:{channel}:VerifyToken"] ?? string.Empty;
        if (string.IsNullOrEmpty(expectedToken) || verifyToken != expectedToken)
            return Results.Unauthorized();

        return Results.Ok(challenge);
    }
}
