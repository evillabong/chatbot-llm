using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Mimo.Api.Hubs;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Services;

/// <summary>
/// Implementación de INotificationService usando SignalR.
/// Envía notificaciones en tiempo real a los funcionarios conectados al TicketHub.
/// </summary>
public class SignalRNotificationService(
    IHubContext<TicketHub> ticketHub,
    ILogger<SignalRNotificationService> logger) : INotificationService
{
    /// <summary>
    /// Notifica a todos los funcionarios de un rol sobre sesiones pendientes en cola.
    /// Emite el evento "QueueUpdated" al grupo "role:{roleId}".
    /// </summary>
    public async Task NotifyPendingSessionsAsync(
        Guid roleId, int pendingCount, double avgWaitMinutes, CancellationToken ct = default)
    {
        logger.LogDebug(
            "Notificando {PendingCount} sesiones pendientes al rol {RoleId}", pendingCount, roleId);

        await ticketHub.Clients
            .Group($"role:{roleId}")
            .SendAsync("QueueUpdated", new { pendingCount, avgWaitMinutes }, ct);
    }

    /// <summary>
    /// Notifica a un funcionario específico sobre la asignación de un nuevo ticket.
    /// Emite el evento "TicketAssigned" al grupo "agent:{agentId}".
    /// </summary>
    public async Task NotifyNewAssignmentAsync(
        Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        logger.LogDebug(
            "Notificando asignación de ticket {TicketId} al funcionario {AgentId}", ticketId, agentId);

        await ticketHub.Clients
            .Group($"agent:{agentId}")
            .SendAsync("TicketAssigned", new { ticketId }, ct);
    }
}
