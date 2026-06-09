using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.Ticketing;

/// <summary>
/// Implementación del servicio de ciclo de vida de tickets.
/// </summary>
public class TicketService(TenantDbContext db) : ITicketService
{
    public async Task<Ticket> CreateAsync(
        Guid conversationId, Guid roleId, string? escalationReason, CancellationToken ct = default)
    {
        var ticket = new Ticket
        {
            Id               = Guid.NewGuid(),
            ConversationId   = conversationId,
            AssignedRoleId   = roleId,
            Status           = TicketStatus.InQueue,
            Priority         = TicketPriority.Normal,
            EscalationReason = escalationReason,
            CreatedAt        = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket> AssignToAgentAsync(Guid ticketId, Guid agentId, CancellationToken ct = default)
    {
        var ticket = await GetRequiredAsync(ticketId, ct);
        ticket.AssignedAgentId = agentId;
        ticket.Status          = TicketStatus.Assigned;
        ticket.AssignedAt      = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket> ResolveAsync(Guid ticketId, string? internalNotes, CancellationToken ct = default)
    {
        var ticket = await GetRequiredAsync(ticketId, ct);
        ticket.Status        = TicketStatus.Resolved;
        ticket.InternalNotes = internalNotes;
        ticket.ResolvedAt    = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket> CloseAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await GetRequiredAsync(ticketId, ct);
        ticket.Status = TicketStatus.Closed;
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket> ReopenAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await GetRequiredAsync(ticketId, ct);
        ticket.Status     = TicketStatus.Reopened;
        ticket.ResolvedAt = null;
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public Task<Ticket?> GetByIdAsync(Guid ticketId, CancellationToken ct = default)
        => db.Tickets
            .Include(t => t.TransferRecords)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

    public async Task<IReadOnlyList<Ticket>> GetByRoleAsync(
        Guid roleId, TicketStatus? status, CancellationToken ct = default)
    {
        var q = db.Tickets.Where(t => t.AssignedRoleId == roleId);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        return await q.OrderBy(t => t.CreatedAt).ToListAsync(ct);
    }

    private async Task<Ticket> GetRequiredAsync(Guid id, CancellationToken ct)
        => await db.Tickets.FindAsync([id], ct)
           ?? throw new InvalidOperationException($"Ticket {id} no encontrado.");
}
