using Mimo.Core.Enums;
using Mimo.Core.Tasks;

namespace Mimo.UnitTests.Tasks;

/// <summary>
/// Pruebas de las reglas de ciclo de vida de una tarea (#24): estados terminales y cálculo de la
/// fecha de cierre al cambiar de estado.
/// </summary>
public class WorkTaskStatusRulesTests
{
    [Theory]
    [InlineData(WorkTaskStatus.Done, true)]
    [InlineData(WorkTaskStatus.Cancelled, true)]
    [InlineData(WorkTaskStatus.Pending, false)]
    [InlineData(WorkTaskStatus.InProgress, false)]
    public void IsTerminal_MatchesExpected(WorkTaskStatus status, bool expected)
        => Assert.Equal(expected, WorkTaskStatusRules.IsTerminal(status));

    [Fact]
    public void ResolveCompletedAt_SetsNow_WhenEnteringTerminal()
    {
        var now = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now, WorkTaskStatusRules.ResolveCompletedAt(WorkTaskStatus.Done, null, now));
    }

    [Fact]
    public void ResolveCompletedAt_PreservesExisting_WhenAlreadyTerminal()
    {
        var earlier = new DateTime(2026, 6, 19, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(earlier, WorkTaskStatusRules.ResolveCompletedAt(WorkTaskStatus.Cancelled, earlier, now));
    }

    [Fact]
    public void ResolveCompletedAt_Clears_WhenReopened()
    {
        var earlier = new DateTime(2026, 6, 19, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
        Assert.Null(WorkTaskStatusRules.ResolveCompletedAt(WorkTaskStatus.InProgress, earlier, now));
    }
}
