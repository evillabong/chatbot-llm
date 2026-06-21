namespace Mimo.Core.DTOs.Knowledge;

/// <summary>
/// Vacío de conocimiento detectado (#22, Fase 1): una consulta cuya recuperación semántica quedó por
/// debajo del umbral. Insumo para la futura bandeja de curación de sugerencias de conocimiento.
/// </summary>
public record KnowledgeGapResponse(
    Guid Id,
    Guid ConversationId,
    string Query,
    double? TopSimilarity,
    int MatchCount,
    DateTime CreatedAt
);
