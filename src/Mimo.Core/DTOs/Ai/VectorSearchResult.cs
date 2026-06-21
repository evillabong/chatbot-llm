namespace Mimo.Core.DTOs.AI;

/// <summary>
/// Resultado de una búsqueda semántica con su señal de recuperación (#22): además de los documentos
/// recuperados, expone la mejor similitud (1 - distancia coseno) para instrumentar vacíos de conocimiento.
/// </summary>
public sealed record VectorSearchResult(
    IReadOnlyList<Mimo.Core.Models.Document> Documents,
    double? TopSimilarity,
    int MatchCount);
