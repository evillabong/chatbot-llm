using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Lógica de asignación automática de tickets a funcionarios disponibles.
/// </summary>
public interface IAgentAssignmentService
{
    /// <summary>
    /// Selecciona el funcionario más adecuado del rol según el modo de asignación configurado.
    /// Retorna null si no hay funcionarios disponibles.
    /// </summary>
    Task<Agent?> SelectAgentForTicketAsync(Guid ticketId, Guid roleId, CancellationToken ct = default);

    /// <summary>Verifica si un funcionario tiene capacidad para tomar más sesiones.</summary>
    Task<bool> HasCapacityAsync(Guid agentId, CancellationToken ct = default);
}
