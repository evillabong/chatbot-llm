using System.Text.RegularExpressions;

namespace Mimo.Core.Automation;

/// <summary>
/// Sustitución simple de marcadores <c>{campo}</c> por valores del evento (#24). Un marcador sin dato
/// se deja tal cual. Pura y acotada (sin recursión) para usarse en títulos de tareas.
/// </summary>
public static partial class TemplateRenderer
{
    [GeneratedRegex(@"\{(?<key>[A-Za-z0-9_]{1,64})\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    public static string Render(string template, IReadOnlyDictionary<string, string?> data)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return PlaceholderRegex().Replace(template, m =>
        {
            var key = m.Groups["key"].Value;
            return data.TryGetValue(key, out var value) && value is not null ? value : m.Value;
        });
    }
}
