using Mimo.Api.Middleware;
using Mimo.Core.DTOs.Conversation;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Webhooks;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Endpoints REST para conversaciones del WebChat.
/// El flujo en tiempo real (mensajes instantáneos) usa ChatHub (SignalR).
/// Estos endpoints permiten iniciar conversación, enviar mensajes y consultar historial.
/// </summary>
public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations")
            .WithTags("Conversations");

        // Iniciar conversación (no requiere auth — ciudadano anónimo)
        group.MapPost("/", StartConversationAsync)
            .WithName("StartConversation")
            .WithSummary("Inicia o retoma una conversación activa del ciudadano.")
            .AllowAnonymous();

        // Enviar mensaje y obtener respuesta del bot (no requiere auth)
        group.MapPost("/messages", SendMessageAsync)
            .WithName("SendMessage")
            .WithSummary("Envía un mensaje y obtiene la respuesta del bot (query: id).")
            .AllowAnonymous();

        // Historial de mensajes
        group.MapGet("/detail", GetConversationAsync)
            .WithName("GetConversation")
            .WithSummary("Obtiene el estado y los mensajes recientes de una conversación (query: id).")
            .AllowAnonymous()
            .Produces<ConversationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> StartConversationAsync(
        StartConversationRequest request,
        IConversationRepository repo,
        IWebhookPublisher webhooks,
        HttpContext context,
        CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();

        // Buscar conversación activa existente para el mismo usuario y canal
        var existing = await repo.GetByExternalUserAsync(request.ExternalUserId, request.Channel, ct);
        if (existing is not null)
            return Results.Ok(ToResponse(existing, []));

        var conversation = new Conversation
        {
            Id               = Guid.NewGuid(),
            TenantId         = tenantId,
            ExternalUserId   = request.ExternalUserId,
            ExternalUserName = request.ExternalUserName,
            Channel          = request.Channel,
            Status           = TicketStatus.BotActive,
            CreatedAt        = DateTime.UtcNow,
            LastMessageAt    = DateTime.UtcNow
        };

        await repo.AddAsync(conversation, ct);
        await webhooks.PublishAsync(WebhookEventTypes.ConversationCreated, new
        {
            id               = conversation.Id,
            channel          = conversation.Channel.ToString(),
            externalUserId   = conversation.ExternalUserId,
            externalUserName = conversation.ExternalUserName,
            createdAt        = conversation.CreatedAt
        }, ct);

        return Results.Created($"/conversations/detail?id={conversation.Id}", ToResponse(conversation, []));
    }

    private static async Task<IResult> SendMessageAsync(
        Guid id,
        SendMessageRequest request,
        IConversationOrchestrator orchestrator,
        IConversationRepository repo,
        CancellationToken ct = default)
    {
        var conversation = await repo.GetByIdAsync(id, ct);
        if (conversation is null)
            return Results.NotFound(new { error = "Conversación no encontrada." });

        // Solo el bot responde mientras el status sea BotActive
        if (conversation.Status != TicketStatus.BotActive)
            return Results.BadRequest(new { error = "Esta conversación no está en modo bot. Usa SignalR para mensajes en vivo." });

        var response = await orchestrator.HandleIncomingMessageAsync(id, request.Content, ct);

        return Results.Ok(new MessageResponse(
            response.Id,
            response.Role,
            response.Content,
            response.CreatedAt));
    }

    private static async Task<IResult> GetConversationAsync(
        Guid id,
        IConversationRepository repo,
        CancellationToken ct = default)
    {
        var conversation = await repo.GetByIdAsync(id, ct);
        if (conversation is null)
            return Results.NotFound(new { error = "Conversación no encontrada." });

        var messages = await repo.GetMessagesAsync(id, limit: 50, ct);
        return Results.Ok(ToResponse(conversation, messages));
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static ConversationResponse ToResponse(
        Conversation c, IReadOnlyList<Message> messages) =>
        new(c.Id, c.Channel, c.Status, c.IsAuthenticated, c.CreatedAt,
            messages.Select(m => new MessageResponse(m.Id, m.Role, m.Content, m.CreatedAt)).ToList(),
            c.ExternalUserName, c.CustomerEmail, c.CustomerPhone, c.LastMessageAt);
}
