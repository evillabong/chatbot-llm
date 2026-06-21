namespace Mimo.Core.Enums;

/// <summary>
/// Estado de una tarea operativa (#24, automatización — entidad Tarea).
/// </summary>
public enum WorkTaskStatus
{
    /// <summary>Pendiente de iniciar.</summary>
    Pending = 0,

    /// <summary>En curso.</summary>
    InProgress = 1,

    /// <summary>Completada.</summary>
    Done = 2,

    /// <summary>Cancelada.</summary>
    Cancelled = 3
}
