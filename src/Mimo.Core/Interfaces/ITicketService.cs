using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Gestión del ciclo de vida de tickets: creación, asignación, resolución y cierre.
/// </summary>
public interface ITicketService
{
    Task<Ticket> CreateAsync(Guid conversationId, Guid roleId, string? escalationReason, CancellationToken ct = default);
    Task<Ticket> AssignToAgentAsync(Guid ticketId, Guid agentId, CancellationToken ct = default);
    Task<Ticket> ResolveAsync(Guid ticketId, string? internalNotes, CancellationToken ct = default);
    Task<Ticket> CloseAsync(Guid ticketId, CancellationToken ct = default);
    Task<Ticket> ReopenAsync(Guid ticketId, CancellationToken ct = default);
    Task<Ticket?> GetByIdAsync(Guid ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> GetByRoleAsync(Guid roleId, TicketStatus? status, CancellationToken ct = default);
}
