using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Gestión de la cola de espera de tickets por rol.
/// </summary>
public interface ITicketQueueService
{
    /// <summary>Agrega un ticket a la cola de un rol.</summary>
    Task EnqueueAsync(Guid ticketId, Guid roleId, CancellationToken ct = default);

    /// <summary>Retira el siguiente ticket de la cola de un rol.</summary>
    Task<Ticket?> DequeueAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>Devuelve los tickets actualmente en cola para un rol.</summary>
    Task<IReadOnlyList<Ticket>> GetQueueAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>Retorna la posición de un ticket en la cola.</summary>
    Task<int> GetPositionAsync(Guid ticketId, CancellationToken ct = default);
}
