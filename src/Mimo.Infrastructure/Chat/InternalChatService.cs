using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.Chat;

/// <summary>
/// Servicio de chat interno entre funcionarios del mismo tenant.
/// Solo gestiona persistencia; la notificación en tiempo real se delega al hub SignalR
/// mediante INotificationService desde la capa de API.
/// </summary>
public class InternalChatService(TenantDbContext db) : IInternalChatService
{
    public async Task<InternalChatMessage> SendAsync(
        Guid fromAgentId,
        Guid toAgentId,
        string content,
        Guid? relatedTicketId,
        CancellationToken ct = default)
    {
        var message = new InternalChatMessage
        {
            Id              = Guid.NewGuid(),
            TenantId        = Guid.Empty, // se establece via search_path; solo se persiste
            FromAgentId     = fromAgentId,
            ToAgentId       = toAgentId,
            RelatedTicketId = relatedTicketId,
            Content         = content,
            IsRead          = false,
            CreatedAt       = DateTime.UtcNow
        };

        db.InternalChatMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<IReadOnlyList<InternalChatMessage>> GetHistoryAsync(
        Guid agentId, Guid otherAgentId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 50;

        return await db.InternalChatMessages
            .AsNoTracking()
            .Where(m =>
                (m.FromAgentId == agentId   && m.ToAgentId == otherAgentId) ||
                (m.FromAgentId == otherAgentId && m.ToAgentId == agentId))
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt) // cronológico al retornar
            .ToListAsync(ct);
    }

    public async Task MarkAsReadAsync(Guid agentId, Guid fromAgentId, CancellationToken ct = default)
    {
        // Marcar como leídos todos los mensajes no leídos enviados por fromAgentId a agentId
        await db.InternalChatMessages
            .Where(m => m.ToAgentId == agentId && m.FromAgentId == fromAgentId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), ct);
    }
}
