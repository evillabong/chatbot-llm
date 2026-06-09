using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Orchestration;

/// <summary>
/// Orquestador central del flujo de conversación.
///
/// Flujo:
///  1. Persiste el mensaje del ciudadano.
///  2. Si la conversación ya está en atención humana, no llama al LLM.
///  3. Busca documentos relevantes filtrados por visibilidad.
///  4. Llama al LLM con RAG y prompt con instrucciones de escalada.
///  5. Parsea si el LLM incluyó [ESCALATE: motivo] en la respuesta.
///  6. Si hay escalada → crea ticket vía McpToolProvider, notifica.
///  7. Persiste y retorna la respuesta limpia.
///
/// Regla de privacidad:
///   El LLM NUNCA decide qué información mostrar. El sistema filtra antes de llamar al LLM.
/// </summary>
public partial class ConversationOrchestrator : IConversationOrchestrator
{
    private readonly IConversationRepository _conversations;
    private readonly IVectorSearchService    _vectorSearch;
    private readonly ILlmClient             _llm;
    private readonly ITicketService         _tickets;
    private readonly IMcpToolProvider       _mcp;
    private readonly ILogger<ConversationOrchestrator> _logger;

    // Token que el LLM incluye cuando solicita escalada a humano
    private static readonly Regex EscalatePattern =
        new(@"\[ESCALATE:\s*(?<reason>[^\]]{1,200})\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ConversationOrchestrator(
        IConversationRepository conversations,
        IVectorSearchService    vectorSearch,
        ILlmClient              llm,
        ITicketService          tickets,
        IMcpToolProvider        mcp,
        ILogger<ConversationOrchestrator> logger)
    {
        _conversations = conversations;
        _vectorSearch  = vectorSearch;
        _llm           = llm;
        _tickets       = tickets;
        _mcp           = mcp;
        _logger        = logger;
    }

    /// <inheritdoc />
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

        // Si ya hay un funcionario asignado, no invocar el bot
        if (conversation.Status != TicketStatus.BotActive)
        {
            _logger.LogDebug(
                "Conversación {Id} en estado {Status}, bot inactivo", conversationId, conversation.Status);
            return userMessage;
        }

        // Búsqueda semántica de documentos relevantes (filtrado por visibilidad ANTES del LLM)
        var relevantDocs = await _vectorSearch.SearchAsync(
            query:           content,
            tenantId:        conversation.TenantId,
            isAuthenticated: conversation.IsAuthenticated,
            topK:            5,
            ct:              ct);

        var history = await _conversations.GetMessagesAsync(conversationId, limit: 10, ct);

        var systemPrompt = BuildSystemPrompt(relevantDocs, conversation.IsAuthenticated);

        var llmHistory = history
            .Where(m => m.Role != MessageRole.System)
            .Select(m => (m.Role == MessageRole.Agent ? "assistant" : m.Role.ToString().ToLower(), m.Content))
            .ToList();

        // ── Llamada al LLM ────────────────────────────────────────────────────
        string rawResponse;
        try
        {
            rawResponse = await _llm.ChatAsync(systemPrompt, llmHistory, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al llamar al LLM para conversación {Id}", conversationId);
            rawResponse = "Lo siento, en este momento no puedo procesar tu consulta. Por favor intenta nuevamente.";
        }

        // ── Detección de escalada ─────────────────────────────────────────────
        string visibleContent = rawResponse;
        string? escalationReason = null;

        var match = EscalatePattern.Match(rawResponse);
        if (match.Success)
        {
            escalationReason = match.Groups["reason"].Value.Trim();
            visibleContent   = EscalatePattern.Replace(rawResponse, string.Empty).Trim();

            _logger.LogInformation(
                "Escalada detectada en conversación {Id}. Motivo: {Reason}", conversationId, escalationReason);

            // Crear ticket y encolarlo vía McpToolProvider (orquesta toda la lógica)
            try
            {
                await _mcp.RequestHumanAgentAsync(conversationId, escalationReason, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al escalar conversación {Id}", conversationId);
            }
        }

        // ── Persistir respuesta del asistente ─────────────────────────────────
        var metadata = JsonSerializer.Serialize(new
        {
            docsUsed      = relevantDocs.Select(d => new { d.Id, d.Title }).ToArray(),
            wasEscalated  = escalationReason is not null,
            escalationReason
        });

        var assistantMessage = new Message
        {
            Id             = Guid.NewGuid(),
            ConversationId = conversationId,
            Role           = MessageRole.Assistant,
            Content        = visibleContent,
            Metadata       = metadata,
            CreatedAt      = DateTime.UtcNow
        };
        await _conversations.AddMessageAsync(assistantMessage, ct);

        return assistantMessage;
    }

    /// <inheritdoc />
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

        await _tickets.CreateAsync(
            ticket.ConversationId, toRoleId,
            $"Transferido desde rol {ticket.AssignedRoleId}. {reason}", ct);

        _logger.LogInformation(
            "Ticket {TicketId} transferido a rol {RoleId} por funcionario {AgentId}",
            ticketId, toRoleId, transferredBy);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye el system prompt con los documentos ya filtrados.
    /// Incluye instrucciones explícitas de escalada para que el LLM
    /// señalice cuando no puede ayudar con el contexto disponible.
    /// </summary>
    private static string BuildSystemPrompt(IReadOnlyList<Document> docs, bool isAuthenticated)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Eres un asistente virtual de atención ciudadana. Responde de forma clara, concisa y profesional.");
        sb.AppendLine("Usa únicamente la información del contexto para responder.");
        sb.AppendLine();
        sb.AppendLine("## Regla de escalada");
        sb.AppendLine("Si NO puedes resolver la consulta con la información disponible, o el ciudadano");
        sb.AppendLine("solicita explícitamente atención humana, añade al final de tu respuesta:");
        sb.AppendLine("  [ESCALATE: <motivo_breve_máx_100_chars>]");
        sb.AppendLine("No menciones este mecanismo al ciudadano. La escalada ocurre en segundo plano.");
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
            sb.AppendLine("No hay información específica disponible sobre la consulta.");
        }

        if (!isAuthenticated)
            sb.AppendLine("\nNota interna: ciudadano no autenticado. Solo se incluyó información pública.");

        return sb.ToString();
    }
}
