using Mimo.Core.Automation;

namespace Mimo.UnitTests.Automation;

/// <summary>
/// Pruebas de la evaluación de condiciones (#24): AND sobre campos del evento, sin condiciones siempre
/// coincide, y la sustitución de marcadores en plantillas.
/// </summary>
public class RuleEvaluatorTests
{
    private static Dictionary<string, string?> Data() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["channel"] = "WhatsApp",
        ["externalUserId"] = "u-123",
        ["id"] = "00000000-0000-0000-0000-000000000abc"
    };

    [Fact]
    public void Matches_NoConditions_AlwaysTrue()
        => Assert.True(RuleEvaluator.Matches([], Data()));

    [Fact]
    public void Matches_AllConditionsMet_True()
    {
        var conditions = new List<RuleCondition> { new("channel", "whatsapp"), new("externalUserId", "u-123") };
        Assert.True(RuleEvaluator.Matches(conditions, Data()));
    }

    [Fact]
    public void Matches_OneConditionFails_False()
    {
        var conditions = new List<RuleCondition> { new("channel", "telegram") };
        Assert.False(RuleEvaluator.Matches(conditions, Data()));
    }

    [Fact]
    public void Matches_MissingField_False()
    {
        var conditions = new List<RuleCondition> { new("nope", "x") };
        Assert.False(RuleEvaluator.Matches(conditions, Data()));
    }

    [Fact]
    public void Render_SubstitutesKnownPlaceholders_AndLeavesUnknown()
    {
        var result = TemplateRenderer.Render("Nuevo chat de {externalUserId} via {channel} ({missing})", Data());
        Assert.Equal("Nuevo chat de u-123 via WhatsApp ({missing})", result);
    }
}
