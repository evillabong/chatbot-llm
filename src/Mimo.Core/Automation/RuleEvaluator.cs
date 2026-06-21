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
            if (!data.TryGetValue(c.Field, out var actual))
                return false;
            if (!string.Equals(actual?.Trim(), c.Value?.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
