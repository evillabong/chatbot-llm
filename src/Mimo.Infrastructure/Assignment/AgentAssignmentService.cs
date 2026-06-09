using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.Assignment;

/// <summary>
/// Asigna tickets a funcionarios disponibles según el modo configurado.
///
/// Manual         → no asigna automáticamente; el funcionario toma la sesión desde la cola.
/// AutomaticBalanced → selecciona el funcionario con menos sesiones activas (least-busy).
/// </summary>
public class AgentAssignmentService(TenantDbContext db) : IAgentAssignmentService
{
    public async Task<Agent?> SelectAgentForTicketAsync(
        Guid ticketId, Guid roleId, CancellationToken ct = default)
    {
        // Obtener funcionarios activos del rol con su carga de trabajo actual
        var agents = await db.Agents
            .AsNoTracking()
            .Where(a => a.IsActive && a.AgentRoles.Any(ar => ar.RoleId == roleId))
            .Select(a => new
            {
                Agent        = a,
                ActiveTickets = db.Tickets.Count(t =>
                    t.AssignedAgentId == a.Id &&
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed)
            })
            .ToListAsync(ct);

        if (agents.Count == 0)
            return null;

        // Seleccionar el funcionario con menor carga que tenga capacidad disponible
        var selected = agents
            .Where(a => a.ActiveTickets < a.Agent.MaxConcurrentSessions)
            .OrderBy(a => a.ActiveTickets)
            .FirstOrDefault();

        return selected?.Agent;
    }

    public async Task<bool> HasCapacityAsync(Guid agentId, CancellationToken ct = default)
    {
        var agent = await db.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.Id == agentId, ct);
        if (agent is null || !agent.IsActive) return false;

        var activeSessions = await db.Tickets.CountAsync(t =>
            t.AssignedAgentId == agentId &&
            t.Status != TicketStatus.Resolved &&
            t.Status != TicketStatus.Closed, ct);

        return activeSessions < agent.MaxConcurrentSessions;
    }
}
