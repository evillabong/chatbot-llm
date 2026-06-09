using Microsoft.AspNetCore.SignalR;
using Mimo.Core.DTOs.Conversation;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Hubs;

/// <summary>
/// Hub SignalR para mensajes en tiempo real entre ciudadano y bot/funcionario.
/// Los clientes se unen al grupo de su conversación al conectarse.
///
/// Eventos emitidos al cliente:
///   - "MessageReceived" (MessageResponse) — nuevo mensaje en la conversación
///   - "StatusChanged"   (string)          — cambio de estado de la conversación
///   - "AgentJoined"     (string alias)    — un funcionario tomó la sesión
/// </summary>
public class ChatHub : Hub
{
    private readonly IConversationRepository  _conversations;
    private readonly IConversationOrchestrator _orchestrator;

    public ChatHub(
        IConversationRepository  conversations,
        IConversationOrchestrator orchestrator)
    {
        _conversations = conversations;
        _orchestrator  = orchestrator;
    }

    /// <summary>
    /// El ciudadano se une al grupo de su conversación para recibir mensajes en tiempo real.
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    /// <summary>
    /// El ciudadano abandona el grupo (al cerrar la ventana de chat).
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
    }

    /// <summary>
    /// Recibe un mensaje del ciudadano, llama al orquestador y emite la respuesta
    /// a todos los miembros del grupo (ciudadano + funcionario si está unido).
    /// </summary>
    public async Task SendMessage(string conversationId, string content)
    {
        if (!Guid.TryParse(conversationId, out var convId))
        {
            await Clients.Caller.SendAsync("Error", "ID de conversación inválido.");
            return;
        }

        var conversation = await _conversations.GetByIdAsync(convId);
        if (conversation is null)
        {
            await Clients.Caller.SendAsync("Error", "Conversación no encontrada.");
            return;
        }

        // Emitir el mensaje del ciudadano al grupo inmediatamente
        var userMsg = new MessageResponse(Guid.NewGuid(), MessageRole.User, content, DateTime.UtcNow);
        await Clients.Group(conversationId).SendAsync("MessageReceived", userMsg);

        // Solo el bot responde en estado BotActive
        if (conversation.Status == TicketStatus.BotActive)
        {
            try
            {
                var response = await _orchestrator.HandleIncomingMessageAsync(convId, content);
                var botMsg = new MessageResponse(response.Id, response.Role, response.Content, response.CreatedAt);
                await Clients.Group(conversationId).SendAsync("MessageReceived", botMsg);
            }
            catch
            {
                await Clients.Caller.SendAsync("Error", "Error al procesar el mensaje.");
            }
        }
        // En estado Assigned/InProgress el mensaje ya fue emitido;
        // el funcionario lo verá en su panel (TicketHub).
    }
}
