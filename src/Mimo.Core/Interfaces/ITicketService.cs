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

    /// <summary>Tickets asignados directamente a un funcionario.</summary>
    Task<IReadOnlyList<Ticket>> GetByAgentAsync(Guid agentId, TicketStatus? status, CancellationToken ct = default);

    /// <summary>
    /// Tickets que un funcionario puede visualizar: los de los roles a los que pertenece.
    /// Si alguno de sus roles tiene CanViewAllTickets, devuelve todos los tickets del tenant.
    /// </summary>
    Task<IReadOnlyList<Ticket>> GetVisibleForAgentAsync(Guid agentId, TicketStatus? status, CancellationToken ct = default);
}
