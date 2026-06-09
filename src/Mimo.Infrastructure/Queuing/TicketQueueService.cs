using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;
using StackExchange.Redis;

namespace Mimo.Infrastructure.Queuing;

/// <summary>
/// Implementación de la cola de tickets por rol usando Redis Sorted Sets.
///
/// Diseño de clave:   mimo:queue:{roleId}
/// Score de elemento: (4 - priority) * 1e13 + unixTimeSecs
///   → ZPOPMIN extrae siempre el ticket de mayor prioridad, y el más antiguo en caso de empate.
/// </summary>
public class TicketQueueService(
    IConnectionMultiplexer redis,
    TenantDbContext db) : ITicketQueueService
{
    private static readonly string KeyPrefix = "mimo:queue";

    // ── Escritura ─────────────────────────────────────────────────────────────

    public async Task EnqueueAsync(Guid ticketId, Guid roleId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new InvalidOperationException($"Ticket {ticketId} no encontrado.");

        var score  = ComputeScore(ticket.Priority, ticket.CreatedAt);
        var rdb    = redis.GetDatabase();
        await rdb.SortedSetAddAsync(BuildKey(roleId), ticketId.ToString("N"), score);
    }

    public async Task<Ticket?> DequeueAsync(Guid roleId, CancellationToken ct = default)
    {
        var rdb    = redis.GetDatabase();
        var result = await rdb.SortedSetPopAsync(BuildKey(roleId), Order.Ascending);

        // SortedSetPopAsync retorna null si la cola está vacía
        if (result is null)
            return null;

        if (!Guid.TryParse(result.Value.Element.ToString(), out var ticketId))
            return null;

        return await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
    }

    // ── Lectura ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Ticket>> GetQueueAsync(Guid roleId, CancellationToken ct = default)
    {
        var rdb      = redis.GetDatabase();
        var elements = await rdb.SortedSetRangeByRankAsync(BuildKey(roleId), 0, -1, Order.Ascending);

        if (elements.Length == 0)
            return [];

        // Preservar el orden de la cola (índice en la lista)
        var orderedIds = elements
            .Select((v, i) => (Id: Guid.TryParse(v.ToString(), out var g) ? g : (Guid?)null, Index: i))
            .Where(x => x.Id.HasValue)
            .ToDictionary(x => x.Id!.Value, x => x.Index);

        var tickets = await db.Tickets
            .AsNoTracking()
            .Where(t => orderedIds.Keys.Contains(t.Id))
            .ToListAsync(ct);

        return tickets
            .OrderBy(t => orderedIds.TryGetValue(t.Id, out var idx) ? idx : int.MaxValue)
            .ToList();
    }

    public async Task<int> GetPositionAsync(Guid ticketId, CancellationToken ct = default)
    {
        // Buscar en qué cola está el ticket
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return -1;

        var rdb   = redis.GetDatabase();
        var rank  = await rdb.SortedSetRankAsync(BuildKey(ticket.AssignedRoleId), ticketId.ToString("N"), Order.Ascending);
        return rank.HasValue ? (int)rank.Value + 1 : -1;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula el score del sorted set.
    /// Menor score = mayor prioridad + llegó antes (ZPOPMIN lo extrae primero).
    /// </summary>
    private static double ComputeScore(TicketPriority priority, DateTime enqueuedAt)
    {
        // factor: Urgent=1, High=2, Normal=3, Low=4
        var factor    = 4 - (int)priority;
        var timePart  = new DateTimeOffset(enqueuedAt).ToUnixTimeSeconds();
        return factor * 1e13 + timePart;
    }

    private static string BuildKey(Guid roleId) => $"{KeyPrefix}:{roleId:N}";
}
