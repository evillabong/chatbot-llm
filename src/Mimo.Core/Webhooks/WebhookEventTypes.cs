namespace Mimo.Core.Webhooks;

/// <summary>
/// Catálogo de eventos a los que un tenant puede suscribir un webhook saliente.
/// Los nombres son estables (forman parte del contrato): se envían en el envelope y en la
/// cabecera <c>X-Mimo-Event</c>. Agregar eventos es retrocompatible; renombrar no lo es.
/// </summary>
public static class WebhookEventTypes
{
    public const string ConversationCreated = "conversation.created";
    public const string TicketAssigned      = "ticket.assigned";
    public const string TicketResolved      = "ticket.resolved";
    public const string SurveyRecorded      = "survey.recorded";

    /// <summary>Todos los eventos soportados (para validar suscripciones).</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ConversationCreated, TicketAssigned, TicketResolved, SurveyRecorded
    };
}
