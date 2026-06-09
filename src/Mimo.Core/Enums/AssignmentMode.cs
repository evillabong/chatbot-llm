namespace Mimo.Core.Enums;

/// <summary>
/// Modo de asignación de sesiones a funcionarios.
/// </summary>
public enum AssignmentMode
{
    /// <summary>El funcionario toma manualmente la sesión desde la cola.</summary>
    Manual,

    /// <summary>El sistema asigna automáticamente balanceando la carga entre funcionarios disponibles.</summary>
    AutomaticBalanced
}
