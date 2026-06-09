using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a datos de conversaciones y mensajes dentro del esquema de un tenant.
/// </summary>
public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Conversation?> GetByExternalUserAsync(string externalUserId, Channel channel, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, int limit = 20, CancellationToken ct = default);
    Task<Conversation> AddAsync(Conversation conversation, CancellationToken ct = default);
    Task<Message> AddMessageAsync(Message message, CancellationToken ct = default);
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
