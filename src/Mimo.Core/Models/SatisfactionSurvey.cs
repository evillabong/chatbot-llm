namespace Mimo.Core.Models;

/// <summary>
/// Encuesta de satisfacción respondida por el ciudadano al cerrar un ticket.
/// </summary>
public class SatisfactionSurvey
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>Calificación del 1 al 5.</summary>
    public int Rating { get; set; }

    /// <summary>Comentarios adicionales opcionales del ciudadano.</summary>
    public string? Observations { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
