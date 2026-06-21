using Mimo.Core.Enums;

namespace Mimo.Core.Tasks;

/// <summary>
/// Reglas puras del ciclo de vida de una tarea (#24). Sin dependencias, para probarse en aislamiento
/// y compartir la lógica entre el endpoint y el futuro motor de reglas.
/// </summary>
public static class WorkTaskStatusRules
{
    /// <summary>True si el estado es terminal (la tarea ya no está abierta).</summary>
    public static bool IsTerminal(WorkTaskStatus status)
        => status is WorkTaskStatus.Done or WorkTaskStatus.Cancelled;

    /// <summary>
    /// Calcula el <c>CompletedAt</c> al cambiar de estado: se fija al entrar a un estado terminal
    /// (si no estaba ya marcado) y se limpia al reabrir.
    /// </summary>
    public static DateTime? ResolveCompletedAt(WorkTaskStatus newStatus, DateTime? existingCompletedAt, DateTime now)
    {
        if (IsTerminal(newStatus))
            return existingCompletedAt ?? now;
        return null;
    }
}
