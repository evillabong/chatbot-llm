using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de conversaciones y mensajes dentro del esquema de un tenant.
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly MimoDbContext _db;

    public ConversationRepository(MimoDbContext db) => _db = db;

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt).Take(50))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Conversation?> GetByExternalUserAsync(
        string externalUserId, Channel channel, CancellationToken ct = default)
        => _db.Conversations
            .Where(c => c.ExternalUserId == externalUserId
                     && c.Channel == channel
                     && c.Status != TicketStatus.Closed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        Guid conversationId, int limit = 20, CancellationToken ct = default)
        => await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)   // devolver en orden cronológico
            .ToListAsync(ct);

    public async Task<Conversation> AddAsync(Conversation conversation, CancellationToken ct = default)
    {
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task<Message> AddMessageAsync(Message message, CancellationToken ct = default)
    {
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }

    public Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _db.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
