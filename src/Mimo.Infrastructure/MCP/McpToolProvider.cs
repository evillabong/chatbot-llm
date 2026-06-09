using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;
using System.Text.Json;

namespace Mimo.Infrastructure.Mcp;

/// <summary>
/// Implementación de las herramientas MCP que el LLM puede invocar.
/// Estas herramientas conectan al LLM con el estado del sistema:
/// base de conocimiento, cola de atención, horarios y escalada a humanos.
/// </summary>
public class McpToolProvider(
    TenantDbContext tenantDb,
    GlobalDbContext globalDb,
    IVectorSearchService vectorSearch,
    IDocumentRepository documentRepo,
    ITicketService ticketService,
    ITicketQueueService ticketQueue,
    IAgentAssignmentService agentAssignment,
    INotificationService notifications,
    IConversationRepository conversationRepo,
    ILogger<McpToolProvider> logger) : IMcpToolProvider
{
    public async Task<string> SearchKnowledgeBaseAsync(
        string query, Guid tenantId, bool isAuthenticated, CancellationToken ct = default)
    {
        var docs = await vectorSearch.SearchAsync(query, tenantId, isAuthenticated, topK: 5, ct: ct);

        if (docs.Count == 0)
            return JsonSerializer.Serialize(new { found = false, documents = Array.Empty<object>() });

        var result = docs.Select(d => new
        {
            d.Id,
            d.Title,
            excerpt = d.Content.Length > 500 ? d.Content[..500] + "…" : d.Content,
            d.Visibility
        });

        return JsonSerializer.Serialize(new { found = true, documents = result });
    }

    public async Task<string> GetDocumentByIdAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await documentRepo.GetByIdAsync(documentId, ct);
        if (doc is null or { IsActive: false })
            return JsonSerializer.Serialize(new { found = false });

        return JsonSerializer.Serialize(new
        {
            found   = true,
            doc.Id,
            doc.Title,
            doc.Content,
            doc.Visibility,
            doc.Tags
        });
    }

    public async Task<string> ClassifyIntentAsync(string message, CancellationToken ct = default)
    {
        // Clasificación simple basada en palabras clave.
        // En producción se puede reemplazar con una llamada al LLM con prompt de clasificación.
        var lower = message.ToLowerInvariant();

        var escalationKeywords = new[]
        {
            "funcionario", "humano", "persona", "asesor", "representante",
            "hablar con", "atención personal", "ayuda", "no entiendo", "queja",
            "reclamación", "urgente"
        };

        var intent = escalationKeywords.Any(k => lower.Contains(k))
            ? "request_human"
            : "general_inquiry";

        return JsonSerializer.Serialize(new { intent, message });
    }

    public async Task<bool> CheckBusinessHoursAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await globalDb.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return true; // si no hay config, asumir abierto

        var config = tenant.Configuration.BusinessHours;
        if (!config.BusinessHoursEnabled) return true;

        var now         = DateTime.UtcNow; // en producción se ajusta por timezone del tenant
        var currentDay  = (int)now.DayOfWeek == 0 ? 7 : (int)now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        return config.WorkingDays.Contains(currentDay)
            && currentTime >= config.StartTime
            && currentTime <= config.EndTime;
    }

    public async Task<string> RequestHumanAgentAsync(
        Guid conversationId, string reason, CancellationToken ct = default)
    {
        var conversation = await conversationRepo.GetByIdAsync(conversationId, ct);
        if (conversation is null)
            return JsonSerializer.Serialize(new { success = false, error = "Conversación no encontrada." });

        // Evitar crear ticket duplicado
        if (conversation.Status != TicketStatus.BotActive)
            return JsonSerializer.Serialize(new { success = false, error = "La conversación ya fue escalada." });

        // Determinar el rol de escalada: el de mayor PriorityLevel del tenant
        var escalationRole = await tenantDb.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.PriorityLevel)
            .FirstOrDefaultAsync(ct);

        if (escalationRole is null)
        {
            logger.LogWarning("No hay roles activos para escalar conversación {ConversationId}", conversationId);
            return JsonSerializer.Serialize(new { success = false, error = "No hay roles disponibles para atención." });
        }

        // Crear ticket y encolarlo
        var ticket = await ticketService.CreateAsync(conversationId, escalationRole.Id, reason, ct);
        await ticketQueue.EnqueueAsync(ticket.Id, escalationRole.Id, ct);

        // Intentar asignación automática si está configurado
        var agent = await agentAssignment.SelectAgentForTicketAsync(ticket.Id, escalationRole.Id, ct);
        if (agent is not null)
        {
            await ticketService.AssignToAgentAsync(ticket.Id, agent.Id, ct);
            await notifications.NotifyNewAssignmentAsync(agent.Id, ticket.Id, ct);
        }

        // Actualizar estado de la conversación
        conversation.Status = TicketStatus.InQueue;
        await conversationRepo.UpdateAsync(conversation, ct);
        await conversationRepo.SaveChangesAsync(ct);

        // Notificar a los funcionarios del rol
        await notifications.NotifyPendingSessionsAsync(
            escalationRole.Id,
            pendingCount: 1,
            avgWaitMinutes: 0,
            ct);

        logger.LogInformation(
            "Conversación {ConversationId} escalada. Ticket {TicketId} en rol {RoleId}",
            conversationId, ticket.Id, escalationRole.Id);

        return JsonSerializer.Serialize(new
        {
            success   = true,
            ticketId  = ticket.Id,
            roleId    = escalationRole.Id,
            roleName  = escalationRole.Name,
            agentName = agent?.FullName
        });
    }

    public async Task<string> CheckQueueStatusAsync(
        Guid tenantId, Guid roleId, CancellationToken ct = default)
    {
        var queue        = await ticketQueue.GetQueueAsync(roleId, ct);
        var pendingCount = queue.Count;

        // Estimación del tiempo de espera basada en promedio histórico simple
        var avgWaitMinutes = pendingCount > 0 ? pendingCount * 5 : 0;

        return JsonSerializer.Serialize(new
        {
            roleId,
            pendingCount,
            estimatedWaitMinutes = avgWaitMinutes,
            isEmpty = pendingCount == 0
        });
    }
}
