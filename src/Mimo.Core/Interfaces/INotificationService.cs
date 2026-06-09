namespace Mimo.Core.Interfaces;

/// <summary>
/// Servicio de notificaciones por correo a funcionarios.
/// </summary>
public interface INotificationService
{
    /// <summary>Notifica a los funcionarios de un rol sobre sesiones pendientes en cola.</summary>
    Task NotifyPendingSessionsAsync(Guid roleId, int pendingCount, double avgWaitMinutes, CancellationToken ct = default);

    /// <summary>Notifica a un funcionario sobre la asignación de un nuevo ticket.</summary>
    Task NotifyNewAssignmentAsync(Guid agentId, Guid ticketId, CancellationToken ct = default);
}
