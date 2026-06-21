using Mimo.Core.Enums;
using Mimo.Core.Sales;

namespace Mimo.UnitTests.Sales;

/// <summary>
/// Pruebas de las reglas del pipeline de oportunidades (#26): etapas terminales y cálculo del cierre.
/// </summary>
public class OpportunityStageRulesTests
{
    [Theory]
    [InlineData(OpportunityStage.Won, true)]
    [InlineData(OpportunityStage.Lost, true)]
    [InlineData(OpportunityStage.New, false)]
    [InlineData(OpportunityStage.Qualified, false)]
    [InlineData(OpportunityStage.Proposal, false)]
    public void IsClosed_MatchesExpected(OpportunityStage stage, bool expected)
        => Assert.Equal(expected, OpportunityStageRules.IsClosed(stage));

    [Fact]
    public void ResolveClosedAt_SetsNow_WhenEnteringTerminal()
    {
        var now = new DateTime(2026, 6, 21, 9, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now, OpportunityStageRules.ResolveClosedAt(OpportunityStage.Won, null, now));
    }

    [Fact]
    public void ResolveClosedAt_PreservesExisting_WhenAlreadyClosed()
    {
        var earlier = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 6, 21, 9, 0, 0, DateTimeKind.Utc);
        Assert.Equal(earlier, OpportunityStageRules.ResolveClosedAt(OpportunityStage.Lost, earlier, now));
    }

    [Fact]
    public void ResolveClosedAt_Clears_WhenReopened()
    {
        var earlier = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 6, 21, 9, 0, 0, DateTimeKind.Utc);
        Assert.Null(OpportunityStageRules.ResolveClosedAt(OpportunityStage.Qualified, earlier, now));
    }
}
