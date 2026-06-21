namespace Mimo.Core.DTOs.Knowledge;

/// <summary>
/// Vacío de conocimiento detectado (#22, Fase 1): una consulta cuya recuperación semántica quedó por
/// debajo del umbral. Insumo para la futura bandeja de curación de sugerencias de conocimiento.
/// <para><c>TopSimilarity</c> es no-nullable (0 = sin coincidencias) para que el SDK lo tipe como
/// <c>double</c> y no como <c>UntypedNode</c> (limitación de OpenAPI 3.0 con value types nullables, #18);
/// <c>MatchCount = 0</c> distingue el caso "sin similitud" del de baja similitud.</para>
/// </summary>
public record KnowledgeGapResponse(
    Guid Id,
    Guid ConversationId,
    string Query,
    double TopSimilarity,
    int MatchCount,
    DateTime CreatedAt
);
