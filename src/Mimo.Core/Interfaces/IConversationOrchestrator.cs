using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Orquestador central del flujo de una conversación.
/// Decide si responde el bot, escala a humano o ejecuta herramientas MCP.
/// </summary>
public interface IConversationOrchestrator
{
    /// <summary>
    /// Procesa un mensaje entrante del ciudadano y retorna la respuesta correspondiente.
    /// Filtra el contexto documental antes de invocar el LLM.
    /// </summary>
    Task<Message> HandleIncomingMessageAsync(Guid conversationId, string content, CancellationToken ct = default);

    /// <summary>Transfiere un ticket a otro rol o funcionario.</summary>
    Task TransferTicketAsync(Guid ticketId, Guid toRoleId, Guid? toAgentId, Guid transferredBy, string? reason, string? contextNote, CancellationToken ct = default);
}
