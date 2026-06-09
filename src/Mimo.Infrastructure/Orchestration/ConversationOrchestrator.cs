using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Orchestration;

/// <summary>
/// Orquestador central del flujo de conversación.
/// Recibe el mensaje del ciudadano, filtra el contexto documental según
/// visibilidad y autenticación, llama al LLM y persiste los mensajes.
/// La IA NO decide qué información mostrar: el sistema filtra antes de llamar al LLM.
/// </summary>
public class ConversationOrchestrator : IConversationOrchestrator
{
    private readonly IConversationRepository _conversations;
    private readonly IVectorSearchService    _vectorSearch;
    private readonly ILlmClient              _llm;
    private readonly ITicketService          _tickets;
    private readonly ILogger<ConversationOrchestrator> _logger;

    public ConversationOrchestrator(
        IConversationRepository conversations,
        IVectorSearchService    vectorSearch,
        ILlmClient              llm,
        ITicketService          tickets,
        ILogger<ConversationOrchestrator> logger)
    {
        _conversations = conversations;
        _vectorSearch  = vectorSearch;
        _llm           = llm;
        _tickets       = tickets;
        _logger        = logger;
    }

    /// <summary>
    /// Procesa el mensaje entrante del ciudadano:
    /// 1. Persiste el mensaje del usuario.
    /// 2. Busca documentos relevantes (filtrados por visibilidad).
    /// 3. Construye el prompt con el contexto documental.
    /// 4. Llama al LLM.
    /// 5. Persiste y retorna la respuesta del asistente.
    /// </summary>
    public async Task<Message> HandleIncomingMessageAsync(
        Guid conversationId,
        string content,
        CancellationToken ct = default)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, ct)
            ?? throw new InvalidOperationException($"Conversación {conversationId} no encontrada.");

        // Guardar mensaje del ciudadano
        var userMessage = new Message
        {
            Id             = Guid.NewGuid(),
            ConversationId = conversationId,
            Role           = MessageRole.User,
            Content        = content,
            CreatedAt      = DateTime.UtcNow
        };
        await _conversations.AddMessageAsync(userMessage, ct);

        // Actualizar timestamp de la conversación
        conversation.LastMessageAt = DateTime.UtcNow;
        await _conversations.UpdateAsync(conversation, ct);
        await _conversations.SaveChangesAsync(ct);

        // Obtener historial reciente para el contexto del LLM (últimos 10 mensajes)
        var history = await _conversations.GetMessagesAsync(conversationId, limit: 10, ct);

        // Buscar documentos relevantes filtrados por visibilidad
        var relevantDocs = await _vectorSearch.SearchAsync(
            query:           content,
            tenantId:        conversation.TenantId,
            isAuthenticated: conversation.IsAuthenticated,
            topK:            5,
            ct:              ct);

        // Construir el system prompt con el contexto documental filtrado
        var systemPrompt = BuildSystemPrompt(relevantDocs, conversation.IsAuthenticated);

        // Preparar el historial para el LLM (excluir mensajes de sistema)
        var llmHistory = history
            .Where(m => m.Role != MessageRole.System)
            .Select(m => (m.Role == MessageRole.Agent ? "assistant" : m.Role.ToString().ToLower(), m.Content))
            .ToList();

        // Llamar al LLM con el contexto filtrado
        string assistantContent;
        string? metadata = null;

        try
        {
            assistantContent = await _llm.ChatAsync(systemPrompt, llmHistory, ct);
            metadata = JsonSerializer.Serialize(new
            {
                docsUsed = relevantDocs.Select(d => new { d.Id, d.Title }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al llamar al LLM para conversación {ConversationId}", conversationId);
            assistantContent = "Lo siento, en este momento no puedo procesar tu consulta. Por favor intenta nuevamente.";
        }

        // Persistir la respuesta del asistente
        var assistantMessage = new Message
        {
            Id             = Guid.NewGuid(),
            ConversationId = conversationId,
            Role           = MessageRole.Assistant,
            Content        = assistantContent,
            Metadata       = metadata,
            CreatedAt      = DateTime.UtcNow
        };
        await _conversations.AddMessageAsync(assistantMessage, ct);

        return assistantMessage;
    }

    /// <summary>
    /// Transfiere un ticket a otro rol o funcionario y registra la auditoría.
    /// </summary>
    public async Task TransferTicketAsync(
        Guid ticketId,
        Guid toRoleId,
        Guid? toAgentId,
        Guid transferredBy,
        string? reason,
        string? contextNote,
        CancellationToken ct = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, ct)
            ?? throw new InvalidOperationException($"Ticket {ticketId} no encontrado.");

        var transfer = new TransferRecord
        {
            Id            = Guid.NewGuid(),
            TicketId      = ticketId,
            FromRoleId    = ticket.AssignedRoleId,
            FromAgentId   = ticket.AssignedAgentId,
            ToRoleId      = toRoleId,
            ToAgentId     = toAgentId,
            TransferredBy = transferredBy,
            Reason        = reason,
            ContextNote   = contextNote,
            CreatedAt     = DateTime.UtcNow
        };

        await _tickets.CreateAsync(
            ticket.ConversationId, toRoleId,
            $"Transferido desde rol {ticket.AssignedRoleId}. {reason}",
            ct);

        _logger.LogInformation(
            "Ticket {TicketId} transferido a rol {RoleId} por agente {AgentId}",
            ticketId, toRoleId, transferredBy);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye el system prompt con los documentos relevantes ya filtrados.
    /// El LLM solo recibe información que el sistema ha autorizado mostrar.
    /// </summary>
    private static string BuildSystemPrompt(IReadOnlyList<Document> docs, bool isAuthenticated)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Eres un asistente virtual de atención ciudadana. Responde de forma clara, concisa y profesional.");
        sb.AppendLine("Usa únicamente la información proporcionada en el contexto para responder.");
        sb.AppendLine("Si no puedes responder con la información disponible, indícalo y sugiere solicitar atención humana.");
        sb.AppendLine();

        if (docs.Count > 0)
        {
            sb.AppendLine("## Información disponible:");
            foreach (var doc in docs)
            {
                sb.AppendLine($"### {doc.Title}");
                sb.AppendLine(doc.Content);
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("No se encontró información específica sobre tu consulta en la base de conocimiento.");
        }

        if (!isAuthenticated)
            sb.AppendLine("\nNota: El ciudadano no está autenticado. Solo se ha incluido información pública.");

        return sb.ToString();
    }
}
