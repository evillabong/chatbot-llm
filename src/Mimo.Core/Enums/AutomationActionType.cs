namespace Mimo.Core.Enums;

/// <summary>
/// Tipo de acción de una regla de automatización (#24, motor evento→condición→acción).
/// Por ahora solo "crear tarea"; se ampliará (etiquetar, escalar, disparar encuesta…).
/// </summary>
public enum AutomationActionType
{
    /// <summary>Crea una tarea operativa.</summary>
    CreateTask = 0,

    /// <summary>Escala la conversación a un funcionario (crea ticket y lo encola).</summary>
    Escalate = 1,

    /// <summary>Sincroniza la oportunidad del evento con el CRM externo.</summary>
    SyncCrm = 2
}
