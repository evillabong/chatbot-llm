using System.Data;
using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.Queuing;

/// <summary>
/// Implementación de la cola de tickets por rol usando PostgreSQL.
///
/// La "cola" no es una estructura separada: es simplemente el conjunto de tickets
/// con Status = InQueue, ordenados por Priority DESC (Urgent primero) y CreatedAt ASC
/// (más antiguo primero en caso de empate).
///
/// DequeueAsync usa una transacción Serializable para garantizar que dos agentes
/// no puedan reclamar el mismo ticket simultáneamente, de forma completamente EF Core.
/// </summary>
public class TicketQueueService(TenantDbContext db) : ITicketQueueService
{
    // ── Escritura ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Valida que el ticket exista y esté en estado InQueue.
    /// Con PostgreSQL la "encola" es implícita: el ticket ya vive en la BD.
    /// </summary>
    public async Task EnqueueAsync(Guid ticketId, Guid roleId, CancellationToken ct = default)
    {
        var exists = await db.Tickets
            .AsNoTracking()
            .AnyAsync(t => t.Id == ticketId
                        && t.AssignedRoleId == roleId
                        && t.Status == TicketStatus.InQueue, ct);

        if (!exists)
            throw new InvalidOperationException(
                $"Ticket {ticketId} no encontrado o no está en estado InQueue para el rol {roleId}.");
    }

    /// <summary>
    /// Retira atómicamente el siguiente ticket de la cola de un rol.
    ///
    /// Usa una transacción Serializable: si dos agentes llaman simultáneamente,
    /// uno obtiene el ticket y el otro reintenta y recibe el siguiente (o null).
    /// El ticket queda con Status = Assigned al retornar.
    /// </summary>
    public async Task<Ticket?> DequeueAsync(Guid roleId, CancellationToken ct = default)
    {
        // CreateExecutionStrategy gestiona reintentos ante fallos de serialización
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

            var ticket = await db.Tickets
                .Where(t => t.Status == TicketStatus.InQueue
                         && t.AssignedRoleId == roleId)
                .OrderByDescending(t => t.Priority)   // Urgent (3) primero
                .ThenBy(t => t.CreatedAt)              // más antiguo primero en empate
                .FirstOrDefaultAsync(ct);

            if (ticket is null)
            {
                await tx.CommitAsync(ct);
                return null;
            }

            // Marcar como Assigned dentro de la misma transacción
            ticket.Status     = TicketStatus.Assigned;
            ticket.AssignedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ticket;
        });
    }

    // ── Lectura ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve los tickets en cola para un rol, ordenados por prioridad y tiempo de llegada.
    /// </summary>
    public Task<IReadOnlyList<Ticket>> GetQueueAsync(Guid roleId, CancellationToken ct = default)
        => db.Tickets
            .AsNoTracking()
            .Where(t => t.Status == TicketStatus.InQueue && t.AssignedRoleId == roleId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<Ticket>>(t => t.Result, ct);

    /// <summary>
    /// Retorna la posición 1-based del ticket en la cola de su rol.
    /// Cuenta cuántos tickets están delante (mayor prioridad, o igual prioridad pero más antiguos).
    /// Devuelve -1 si el ticket no existe o ya no está en cola.
    /// </summary>
    public async Task<int> GetPositionAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.Status == TicketStatus.InQueue, ct);

        if (ticket is null) return -1;

        var ahead = await db.Tickets.CountAsync(t =>
            t.Status == TicketStatus.InQueue
         && t.AssignedRoleId == ticket.AssignedRoleId
         && t.Id != ticketId
         && (t.Priority > ticket.Priority
             || (t.Priority == ticket.Priority && t.CreatedAt < ticket.CreatedAt)), ct);

        return ahead + 1;
    }
}
