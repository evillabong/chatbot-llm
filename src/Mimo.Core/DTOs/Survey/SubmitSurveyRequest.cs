namespace Mimo.Core.DTOs.Survey;

/// <summary>Solicitud del ciudadano para enviar una encuesta de satisfacción.</summary>
public record SubmitSurveyRequest(
    /// <summary>Calificación del 1 (muy malo) al 5 (excelente).</summary>
    int Rating,
    /// <summary>Comentarios opcionales.</summary>
    string? Observations
);
