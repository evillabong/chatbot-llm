using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Chat interno entre funcionarios del mismo tenant.
/// </summary>
public interface IInternalChatService
{
    Task<InternalChatMessage> SendAsync(Guid fromAgentId, Guid toAgentId, string content, Guid? relatedTicketId, CancellationToken ct = default);
    Task<IReadOnlyList<InternalChatMessage>> GetHistoryAsync(Guid agentId, Guid otherAgentId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid agentId, Guid fromAgentId, CancellationToken ct = default);
}
