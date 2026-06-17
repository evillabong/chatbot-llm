using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Conversation;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Hubs;

/// <summary>
/// Hub SignalR para funcionarios. Maneja:
///   - Notificaciones de nuevos tickets en la cola del rol
///   - Mensajes enviados por el funcionario al ciudadano
///   - Mensajes de chat interno entre funcionarios
///
/// Grupos de SignalR usados:
///   - "role:{roleId}"         → todos los funcionarios que observan ese rol
///   - "agent:{agentId}"       → notificaciones individuales del funcionario
///   - "conv:{conversationId}" → conversación específica (compartido con ChatHub)
///
/// Eventos emitidos al cliente:
///   - "TicketEnqueued"    (TicketResponse)         — nuevo ticket disponible en cola
///   - "TicketAssigned"    (TicketResponse)         — ticket asignado a este agente
///   - "TicketResolved"    (Guid)                   — ticket cerrado
///   - "AgentMessage"      (MessageResponse)        — mensaje del agente al ciudadano
///   - "InternalMessage"   (InternalMessageResponse) — chat interno recibido
///   - "QueueUpdated"      (int pendingCount)       — cambio en el tamaño de la cola
/// </summary>
[Authorize(Policy = MimoAuthorization.Policies.Agent)]
public class TicketHub(
    ITicketService ticketService,
    IConversationRepository conversations,
    IAgentRepository agents,
    IHubContext<ChatHub> chatHub) : Hub
{
    // ── Gestión de grupos ─────────────────────────────────────────────────────

    /// <summary>
    /// Al conectar, el funcionario se une a su grupo personal "agent:{id}" para recibir
    /// notificaciones dirigidas a él (p. ej. mensajes de chat interno).
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var agentId = GetAgentId();
        if (agentId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"agent:{agentId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// El funcionario se suscribe a la cola de un rol para recibir notificaciones en tiempo real.
    /// </summary>
    public Task JoinRoleQueue(string roleId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"role:{roleId}");

    /// <summary>Deja de observar la cola de un rol.</summary>
    public Task LeaveRoleQueue(string roleId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role:{roleId}");

    /// <summary>
    /// El funcionario se une a la conversación de un ticket para recibir/enviar mensajes al ciudadano.
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
        // También unirse al grupo de ChatHub para ver los mensajes del ciudadano
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    /// <summary>El funcionario abandona la conversación.</summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
    }

    // ── Acciones del funcionario ──────────────────────────────────────────────

    /// <summary>
    /// El funcionario toma un ticket de la cola (asignación manual).
    /// Notifica al ciudadano que un funcionario se unió a la conversación.
    /// </summary>
    public async Task ClaimTicket(string ticketId)
    {
        if (!Guid.TryParse(ticketId, out var tId))
        {
            await Clients.Caller.SendAsync("Error", "ID de ticket inválido.");
            return;
        }

        var agentId = GetAgentId();
        if (agentId is null)
        {
            await Clients.Caller.SendAsync("Error", "No se pudo identificar el funcionario.");
            return;
        }

        var ticket = await ticketService.AssignToAgentAsync(tId, agentId.Value);
        var agent  = await agents.GetByIdAsync(agentId.Value);

        // Notificar al ciudadano en ChatHub
        await chatHub.Clients.Group(ticket.ConversationId.ToString())
            .SendAsync("AgentJoined", agent?.Alias ?? "Funcionario");

        // Notificar al resto del rol que el ticket fue tomado
        var roleGroup = $"role:{ticket.AssignedRoleId}";
        await Clients.Group(roleGroup).SendAsync("TicketAssigned", new
        {
            ticket.Id,
            ticket.ConversationId,
            agentId   = agentId.Value,
            agentName = agent?.FullName
        });
    }

    /// <summary>
    /// El funcionario envía un mensaje al ciudadano.
    /// Se persiste y se emite tanto en TicketHub como en ChatHub.
    /// </summary>
    public async Task SendMessageToCitizen(string conversationId, string content)
    {
        if (!Guid.TryParse(conversationId, out var convId))
        {
            await Clients.Caller.SendAsync("Error", "ID de conversación inválido.");
            return;
        }

        var agentId = GetAgentId();
        if (agentId is null) return;

        var agent = await agents.GetByIdAsync(agentId.Value);

        var message = new Mimo.Core.Models.Message
        {
            Id             = Guid.NewGuid(),
            ConversationId = convId,
            Role           = MessageRole.Agent,
            SenderId       = agentId.Value,
            Content        = content,
            CreatedAt      = DateTime.UtcNow
        };

        // Persistir el mensaje vía repositorio de conversaciones
        var conversation = await conversations.GetByIdAsync(convId);
        if (conversation is null) return;

        await conversations.AddMessageAsync(message);

        // Marcar primer tiempo de respuesta del ticket si aplica
        if (conversation.Ticket is not null && conversation.Ticket.FirstResponseAt is null)
            await ticketService.AssignToAgentAsync(conversation.Ticket.Id, agentId.Value);

        var msgDto = new MessageResponse(message.Id, message.Role, content, message.CreatedAt);

        // Emitir al ciudadano (ChatHub) y al grupo de la conversación en TicketHub
        await chatHub.Clients.Group(conversationId).SendAsync("MessageReceived", msgDto);
        await Clients.Group($"conv:{conversationId}").SendAsync("AgentMessage", msgDto);
    }

    /// <summary>
    /// El funcionario resuelve el ticket y cierra la conversación.
    /// </summary>
    public async Task ResolveTicket(string ticketId, string? notes)
    {
        if (!Guid.TryParse(ticketId, out var tId)) return;

        var ticket = await ticketService.ResolveAsync(tId, notes);

        // Notificar al ciudadano
        await chatHub.Clients.Group(ticket.ConversationId.ToString())
            .SendAsync("StatusChanged", "Resolved");

        // Notificar al grupo del rol
        await Clients.Group($"role:{ticket.AssignedRoleId}")
            .SendAsync("TicketResolved", tId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Extrae el AgentId del claim JWT del funcionario conectado.
    /// El claim se llama "agent_id" y es un GUID.
    /// </summary>
    private Guid? GetAgentId()
    {
        var claim = Context.User?.FindFirst("agent_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
