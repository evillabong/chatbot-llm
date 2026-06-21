namespace Mimo.Core.Automation;

/// <summary>
/// Evaluación pura de las condiciones de una regla (#24) contra los datos de un evento. Sin
/// dependencias, para probarse en aislamiento y compartirse con el dispatcher.
/// </summary>
public static class RuleEvaluator
{
    /// <summary>
    /// True si **todas** las condiciones se cumplen (AND): para cada una, el dato del evento existe y
    /// es igual (ignorando mayúsculas/espacios) al valor esperado. Sin condiciones → siempre coincide.
    /// </summary>
    public static bool Matches(IReadOnlyList<RuleCondition> conditions, IReadOnlyDictionary<string, string?> data)
    {
        if (conditions.Count == 0)
            return true;

        foreach (var c in conditions)
        {
            data.TryGetValue(c.Field, out var raw);
            if (!Evaluate(c.Operator, raw, c.Value))
                return false;
        }
        return true;
    }

    private static bool Evaluate(ConditionOperator op, string? actual, string? expected)
    {
        var a = actual?.Trim();
        var e = expected?.Trim() ?? "";

        return op switch
        {
            ConditionOperator.Equals      => string.Equals(a, e, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.NotEquals   => !string.Equals(a, e, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.Contains    => a is not null && a.Contains(e, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.NotContains => a is null || !a.Contains(e, StringComparison.OrdinalIgnoreCase),
            _                             => false
        };
    }
}
