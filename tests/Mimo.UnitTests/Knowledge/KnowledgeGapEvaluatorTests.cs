using Mimo.Core.Knowledge;

namespace Mimo.UnitTests.Knowledge;

/// <summary>
/// Pruebas de la decisión de "vacío de conocimiento" (#22, Fase 1): sin coincidencias, sin similitud
/// o similitud por debajo del umbral marcan gap; por encima del umbral no.
/// </summary>
public class KnowledgeGapEvaluatorTests
{
    [Fact]
    public void IsGap_NoMatches_IsGap()
        => Assert.True(KnowledgeGapEvaluator.IsGap(topSimilarity: null, matchCount: 0));

    [Fact]
    public void IsGap_NullSimilarityWithMatches_IsGap()
        => Assert.True(KnowledgeGapEvaluator.IsGap(topSimilarity: null, matchCount: 3));

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.74)]
    public void IsGap_SimilarityBelowThreshold_IsGap(double similarity)
        => Assert.True(KnowledgeGapEvaluator.IsGap(similarity, matchCount: 3));

    [Theory]
    [InlineData(0.75)]
    [InlineData(0.9)]
    [InlineData(1.0)]
    public void IsGap_SimilarityAtOrAboveThreshold_IsNotGap(double similarity)
        => Assert.False(KnowledgeGapEvaluator.IsGap(similarity, matchCount: 3));

    [Fact]
    public void IsGap_CustomThreshold_Respected()
    {
        Assert.True(KnowledgeGapEvaluator.IsGap(0.8, matchCount: 2, threshold: 0.85));
        Assert.False(KnowledgeGapEvaluator.IsGap(0.8, matchCount: 2, threshold: 0.75));
    }
}
