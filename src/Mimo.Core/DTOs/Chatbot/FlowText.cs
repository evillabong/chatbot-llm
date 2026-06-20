using System.Text.RegularExpressions;

namespace Mimo.Core.DTOs.Chatbot;

/// <summary>Utilidades de texto de flujos: sustitución de <c>{variable}</c> con las variables capturadas.</summary>
public static partial class FlowText
{
    [GeneratedRegex(@"\{(?<key>[a-zA-Z0-9_]+)\}")]
    private static partial Regex PlaceholderRegex();

    /// <summary>Reemplaza <c>{clave}</c> por su valor; deja intactos los marcadores sin variable.</summary>
    public static string Substitute(string? text, IReadOnlyDictionary<string, string> vars)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('{')) return text ?? string.Empty;
        return PlaceholderRegex().Replace(text, m =>
            vars.TryGetValue(m.Groups["key"].Value, out var v) ? v : m.Value);
    }
}
