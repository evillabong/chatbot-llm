namespace Mimo.Core.DTOs.Survey;

/// <summary>Encuesta de satisfacción registrada.</summary>
public record SurveyResponse(
    Guid Id,
    Guid TicketId,
    int Rating,
    string? Observations,
    DateTime RecordedAt
);
