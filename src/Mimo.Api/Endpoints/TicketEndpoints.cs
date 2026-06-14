using Microsoft.AspNetCore.SignalR;
using Mimo.Api.Hubs;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Ticket;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Endpoints REST para gestión de tickets por funcionarios autenticados.
/// Complementan al TicketHub para acciones que no requieren tiempo real.
/// </summary>
public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tickets")
            .WithTags("Tickets")
            .RequireAuthorization(MimoAuthorization.Policies.Agent);

        // GET /tickets/queue?roleId=
        group.MapGet("/queue", GetRoleQueueAsync)
            .WithName("GetRoleQueue")
            .WithSummary("Lista los tickets en cola para un rol (query: roleId).");

        // GET /tickets/detail?id=
        group.MapGet("/detail", GetTicketAsync)
            .WithName("GetTicket")
            .WithSummary("Obtiene un ticket por ID con su historial de transferencias (query: id).");

        // GET /tickets/my
        group.MapGet("/my", GetMyTicketsAsync)
            .WithName("GetMyTickets")
            .WithSummary("Lista los tickets asignados al funcionario autenticado.");

        // GET /tickets/visible
        group.MapGet("/visible", GetVisibleTicketsAsync)
            .WithName("GetVisibleTickets")
            .WithSummary("Lista los tickets visibles para el funcionario según sus roles (todos si tiene CanViewAllTickets).");

        // POST /tickets/claim?id=
        group.MapPost("/claim", ClaimTicketAsync)
            .WithName("ClaimTicket")
            .WithSummary("El funcionario toma el ticket de la cola / asignación manual (query: id).");

        // POST /tickets/resolve?id=
        group.MapPost("/resolve", ResolveTicketAsync)
            .WithName("ResolveTicket")
            .WithSummary("Marca el ticket como resuelto (query: id).");

        // POST /tickets/close?id=
        group.MapPost("/close", CloseTicketAsync)
            .WithName("CloseTicket")
            .WithSummary("Cierra definitivamente el ticket (query: id).");

        // POST /tickets/reopen?id=
        group.MapPost("/reopen", ReopenTicketAsync)
            .WithName("ReopenTicket")
            .WithSummary("Reabre un ticket resuelto (query: id).");

        // POST /tickets/transfer?id=
        group.MapPost("/transfer", TransferTicketAsync)
            .WithName("TransferTicket")
            .WithSummary("Transfiere el ticket a otro rol o funcionario (query: id).");

        // PATCH /tickets/notes?id=
        group.MapPatch("/notes", UpdateNotesAsync)
            .WithName("UpdateTicketNotes")
            .WithSummary("Actualiza las notas internas del ticket (query: id).");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> GetRoleQueueAsync(
        Guid roleId,
        ITicketQueueService queue,
        CancellationToken ct = default)
    {
        var tickets = await queue.GetQueueAsync(roleId, ct);
        var result  = await Task.WhenAll(tickets.Select(async t =>
        {
            var pos = await queue.GetPositionAsync(t.Id, ct);
            return ToResponse(t, pos);
        }));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTicketAsync(
        Guid id,
        ITicketService service,
        ITicketQueueService queue,
        CancellationToken ct = default)
    {
        var ticket = await service.GetByIdAsync(id, ct);
        if (ticket is null)
            return Results.NotFound(new { error = "Ticket no encontrado." });

        var pos = await queue.GetPositionAsync(id, ct);
        return Results.Ok(ToResponse(ticket, pos));
    }

    private static async Task<IResult> GetMyTicketsAsync(
        HttpContext context,
        ITicketService service,
        TicketStatus? status = null,
        CancellationToken ct = default)
    {
        var agentId = GetAgentId(context);
        if (agentId is null)
            return Results.Unauthorized();

        var tickets = await service.GetByAgentAsync(agentId.Value, status, ct);
        return Results.Ok(tickets.Select(t => ToResponse(t, 0)).ToList());
    }

    private static async Task<IResult> GetVisibleTicketsAsync(
        HttpContext context,
        ITicketService service,
        TicketStatus? status = null,
        CancellationToken ct = default)
    {
        var agentId = GetAgentId(context);
        if (agentId is null)
            return Results.Unauthorized();

        var tickets = await service.GetVisibleForAgentAsync(agentId.Value, status, ct);
        return Results.Ok(tickets.Select(t => ToResponse(t, 0)).ToList());
    }

    private static async Task<IResult> ClaimTicketAsync(
        Guid id,
        HttpContext context,
        ITicketService service,
        ITicketQueueService queue,
        IAgentRepository agentRepo,
        IHubContext<TicketHub> hub,
        IHubContext<ChatHub> chatHub,
        CancellationToken ct = default)
    {
        var agentId = GetAgentId(context);
        if (agentId is null) return Results.Unauthorized();

        var ticket = await service.GetByIdAsync(id, ct);
        if (ticket is null)
            return Results.NotFound(new { error = "Ticket no encontrado." });

        if (ticket.Status != TicketStatus.InQueue)
            return Results.Conflict(new { error = "El ticket ya no está en cola." });

        var updated = await service.AssignToAgentAsync(id, agentId.Value, ct);
        var agent   = await agentRepo.GetByIdAsync(agentId.Value, ct);

        // Notificar al ciudadano en tiempo real
        await chatHub.Clients.Group(ticket.ConversationId.ToString())
            .SendAsync("AgentJoined", agent?.Alias ?? "Funcionario", ct);

        // Notificar al rol
        await hub.Clients.Group($"role:{ticket.AssignedRoleId}")
            .SendAsync("TicketAssigned", new { ticketId = id, agentId = agentId.Value }, ct);

        return Results.Ok(ToResponse(updated, 0));
    }

    private static async Task<IResult> ResolveTicketAsync(
        Guid id,
        UpdateTicketNotesRequest? request,
        ITicketService service,
        IHubContext<TicketHub> hub,
        IHubContext<ChatHub> chatHub,
        CancellationToken ct = default)
    {
        var ticket = await service.ResolveAsync(id, request?.Notes, ct);

        await chatHub.Clients.Group(ticket.ConversationId.ToString())
            .SendAsync("StatusChanged", "Resolved", ct);

        await hub.Clients.Group($"role:{ticket.AssignedRoleId}")
            .SendAsync("TicketResolved", id, ct);

        return Results.Ok(ToResponse(ticket, 0));
    }

    private static async Task<IResult> CloseTicketAsync(
        Guid id,
        ITicketService service,
        IHubContext<ChatHub> chatHub,
        CancellationToken ct = default)
    {
        var ticket = await service.CloseAsync(id, ct);

        await chatHub.Clients.Group(ticket.ConversationId.ToString())
            .SendAsync("StatusChanged", "Closed", ct);

        return Results.Ok(ToResponse(ticket, 0));
    }

    private static async Task<IResult> ReopenTicketAsync(
        Guid id,
        ITicketService service,
        ITicketQueueService queue,
        CancellationToken ct = default)
    {
        var ticket = await service.ReopenAsync(id, ct);
        await queue.EnqueueAsync(id, ticket.AssignedRoleId, ct);
        return Results.Ok(ToResponse(ticket, 1));
    }

    private static async Task<IResult> TransferTicketAsync(
        Guid id,
        TransferTicketRequest request,
        HttpContext context,
        ITicketService service,
        ITicketQueueService queue,
        IAgentAssignmentService assignment,
        INotificationService notifications,
        IHubContext<TicketHub> hub,
        CancellationToken ct = default)
    {
        var agentId = GetAgentId(context);
        if (agentId is null) return Results.Unauthorized();

        var ticket = await service.GetByIdAsync(id, ct);
        if (ticket is null)
            return Results.NotFound(new { error = "Ticket no encontrado." });

        // Registrar transferencia
        var transfer = new TransferRecord
        {
            Id            = Guid.NewGuid(),
            TicketId      = id,
            FromRoleId    = ticket.AssignedRoleId,
            FromAgentId   = ticket.AssignedAgentId,
            ToRoleId      = request.ToRoleId,
            ToAgentId     = request.ToAgentId,
            TransferredBy = agentId.Value,
            Reason        = request.Reason,
            ContextNote   = request.ContextNote,
            IsPartial     = request.IsPartial,
            CreatedAt     = DateTime.UtcNow
        };

        // Crear nuevo ticket en el rol destino (reasignación)
        var newTicket = await service.CreateAsync(ticket.ConversationId, request.ToRoleId,
            $"Transferido: {request.Reason}", ct);
        await queue.EnqueueAsync(newTicket.Id, request.ToRoleId, ct);

        // Si hay agente destino específico, asignar directamente
        if (request.ToAgentId.HasValue)
        {
            await service.AssignToAgentAsync(newTicket.Id, request.ToAgentId.Value, ct);
            await notifications.NotifyNewAssignmentAsync(request.ToAgentId.Value, newTicket.Id, ct);
        }
        else
        {
            // Intentar asignación automática
            var autoAgent = await assignment.SelectAgentForTicketAsync(newTicket.Id, request.ToRoleId, ct);
            if (autoAgent is not null)
            {
                await service.AssignToAgentAsync(newTicket.Id, autoAgent.Id, ct);
                await notifications.NotifyNewAssignmentAsync(autoAgent.Id, newTicket.Id, ct);
            }
        }

        // Notificar al rol destino
        await hub.Clients.Group($"role:{request.ToRoleId}")
            .SendAsync("TicketEnqueued", new { ticketId = newTicket.Id }, ct);

        return Results.Ok(new { message = "Ticket transferido.", newTicketId = newTicket.Id });
    }

    private static async Task<IResult> UpdateNotesAsync(
        Guid id,
        UpdateTicketNotesRequest request,
        ITicketService service,
        CancellationToken ct = default)
    {
        var ticket = await service.GetByIdAsync(id, ct);
        if (ticket is null)
            return Results.NotFound(new { error = "Ticket no encontrado." });

        await service.ResolveAsync(id, request.Notes, ct); // reutiliza para actualizar notas
        return Results.NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Guid? GetAgentId(HttpContext context)
    {
        var claim = context.User?.FindFirst("agent_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static TicketResponse ToResponse(Ticket t, int queuePosition) =>
        new(t.Id, t.ConversationId, t.AssignedRoleId, t.AssignedAgentId,
            t.Priority, t.Status, t.EscalationReason, t.InternalNotes,
            t.CreatedAt, t.AssignedAt, t.FirstResponseAt, t.ResolvedAt,
            queuePosition);
}
