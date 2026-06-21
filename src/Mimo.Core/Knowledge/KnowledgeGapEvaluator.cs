namespace Mimo.Core.Knowledge;

/// <summary>
/// Decisión pura de "vacío de conocimiento" (#22, Fase 1) a partir de la señal de recuperación.
/// Sin dependencias para poder probarse en aislamiento y reutilizarse desde el orquestador.
/// </summary>
public static class KnowledgeGapEvaluator
{
    /// <summary>
    /// Umbral por defecto de similitud (1 - distancia coseno). Por debajo se considera que la
    /// recuperación no fue suficientemente buena y la consulta marca un posible vacío.
    /// </summary>
    public const double DefaultSimilarityThreshold = 0.75;

    /// <summary>
    /// True si la consulta representa un vacío de conocimiento: no hubo coincidencias, no hay
    /// similitud, o la mejor similitud quedó por debajo del umbral.
    /// </summary>
    public static bool IsGap(double? topSimilarity, int matchCount, double threshold = DefaultSimilarityThreshold)
        => matchCount <= 0 || topSimilarity is null || topSimilarity.Value < threshold;
}
