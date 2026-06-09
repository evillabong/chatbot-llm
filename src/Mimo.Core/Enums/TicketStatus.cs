namespace Mimo.Core.Enums;

/// <summary>
/// Estados posibles de un ticket de atención al ciudadano.
/// </summary>
public enum TicketStatus
{
    /// <summary>El bot está atendiendo la conversación.</summary>
    BotActive,

    /// <summary>El ciudadano solicitó atención humana y espera en cola.</summary>
    InQueue,

    /// <summary>Un funcionario tomó el ticket pero aún no inició atención activa.</summary>
    Assigned,

    /// <summary>El funcionario está atendiendo activamente al ciudadano.</summary>
    InProgress,

    /// <summary>Nadie tomó el ticket dentro del tiempo límite.</summary>
    Unattended,

    /// <summary>El ticket fue resuelto; pendiente de encuesta si aplica.</summary>
    Resolved,

    /// <summary>El ciudadano está respondiendo la encuesta de satisfacción.</summary>
    Survey,

    /// <summary>El ticket está cerrado definitivamente.</summary>
    Closed,

    /// <summary>El ticket fue reabierto por baja calificación en la encuesta.</summary>
    Reopened
}
