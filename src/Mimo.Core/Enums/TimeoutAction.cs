namespace Mimo.Core.Enums;

/// <summary>
/// Acción a ejecutar cuando una sesión no es atendida dentro del tiempo límite.
/// </summary>
public enum TimeoutAction
{
    /// <summary>Marcar la sesión como no atendida y cerrarla.</summary>
    MarkAsUnattended,

    /// <summary>Transferir automáticamente a otro rol configurado.</summary>
    TransferToAnotherRole,

    /// <summary>Notificar al administrador del tenant.</summary>
    NotifyAdmin
}
