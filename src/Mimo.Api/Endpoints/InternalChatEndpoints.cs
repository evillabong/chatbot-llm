using Microsoft.AspNetCore.SignalR;
using Mimo.Api.Hubs;
using Mimo.Core.DTOs.InternalChat;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Endpoints REST para el chat interno entre funcionarios.
/// Las notificaciones en tiempo real se envían vía TicketHub.
/// </summary>
public static class InternalChatEndpoints
{
    public static IEndpointRouteBuilder MapInternalChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal-chat")
            .WithTags("InternalChat")
            .RequireAuthorization();

        // GET /internal-chat/{agentId}/history
        group.MapGet("/{agentId:guid}/history", GetHistoryAsync)
            .WithName("GetInternalChatHistory")
            .WithSummary("Historial paginado de mensajes con otro funcionario.");

        // POST /internal-chat/send
        group.MapPost("/send", SendMessageAsync)
            .WithName("SendInternalMessage")
            .WithSummary("Envía un mensaje de chat interno a otro funcionario.");

        // POST /internal-chat/{agentId}/read
        group.MapPost("/{agentId:guid}/read", MarkAsReadAsync)
            .WithName("MarkInternalMessagesRead")
            .WithSummary("Marca como leídos los mensajes de un funcionario.");

        return app;
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid agentId,
        HttpContext context,
        IInternalChatService chatService,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var myId = GetAgentId(context);
        if (myId is null) return Results.Unauthorized();

        var messages = await chatService.GetHistoryAsync(myId.Value, agentId, page, pageSize, ct);
        return Results.Ok(messages.Select(ToResponse).ToList());
    }

    private static async Task<IResult> SendMessageAsync(
        SendInternalMessageRequest request,
        HttpContext context,
        IInternalChatService chatService,
        IHubContext<TicketHub> hub,
        CancellationToken ct = default)
    {
        var fromId = GetAgentId(context);
        if (fromId is null) return Results.Unauthorized();

        var message = await chatService.SendAsync(
            fromId.Value, request.ToAgentId, request.Content, request.RelatedTicketId, ct);

        var dto = ToResponse(message);

        // Notificar al destinatario en tiempo real via TicketHub
        await hub.Clients
            .Group($"agent:{request.ToAgentId}")
            .SendAsync("InternalMessage", dto, ct);

        return Results.Created($"/internal-chat/{request.ToAgentId}/history", dto);
    }

    private static async Task<IResult> MarkAsReadAsync(
        Guid agentId,
        HttpContext context,
        IInternalChatService chatService,
        CancellationToken ct = default)
    {
        var myId = GetAgentId(context);
        if (myId is null) return Results.Unauthorized();

        await chatService.MarkAsReadAsync(myId.Value, agentId, ct);
        return Results.NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Guid? GetAgentId(HttpContext context)
    {
        var claim = context.User?.FindFirst("agent_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static InternalMessageResponse ToResponse(Mimo.Core.Models.InternalChatMessage m) =>
        new(m.Id, m.FromAgentId, m.ToAgentId, m.RelatedTicketId,
            m.Content, m.IsRead, m.CreatedAt);
}
