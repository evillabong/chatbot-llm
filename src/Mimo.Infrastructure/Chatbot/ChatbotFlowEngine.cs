using System.Text;
using System.Text.RegularExpressions;
using Mimo.Core.DTOs.Chatbot;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Chatbot;

/// <summary>
/// Motor determinista del chatbot por opciones (#27). Lógica pura sin BD: recorre el árbol de nodos
/// (mensaje → continúa solo; menú → espera elección; input → captura/valida un dato; escalar → deriva
/// a funcionario). Sustituye <c>{variable}</c> en los textos con las variables capturadas.
/// </summary>
public sealed class ChatbotFlowEngine : IChatbotFlowEngine
{
    // Límite a la evaluación de las regex de validación (provienen del flujo del tenant): evita ReDoS.
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly Regex PlaceholderRegex = new(@"\{(?<key>[a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    public FlowStepResult Process(
        FlowDefinition flow, string? currentNodeId, IReadOnlyDictionary<string, string> variables, string input)
    {
        var nodes = new Dictionary<string, FlowNode>(StringComparer.Ordinal);
        foreach (var n in flow.Nodes) nodes[n.Id] = n;

        var vars = new Dictionary<string, string>(variables, StringComparer.Ordinal);

        // Inicio del flujo: arrancar en el nodo de entrada (se ignora la entrada del usuario).
        if (string.IsNullOrEmpty(currentNodeId))
            return Resolve(nodes, flow.EntryNodeId, vars);

        if (!nodes.TryGetValue(currentNodeId, out var current))
            return Resolve(nodes, flow.EntryNodeId, vars);

        var answer = (input ?? string.Empty).Trim();

        switch (current.Type)
        {
            case FlowNodeType.Menu:
                var option = current.Options?.FirstOrDefault(o =>
                    string.Equals(o.Key, answer, StringComparison.OrdinalIgnoreCase));
                if (option is null)
                    return Stay(current, vars, $"No entendí tu opción. {RenderMenu(current, vars)}");
                return Resolve(nodes, option.Next, vars);

            case FlowNodeType.Input:
                if (!IsValid(current, answer))
                {
                    var err = string.IsNullOrWhiteSpace(current.ValidationError)
                        ? "El dato no es válido, inténtalo de nuevo."
                        : Substitute(current.ValidationError, vars);
                    return Stay(current, vars, $"{err}\n{Substitute(current.Text, vars)}");
                }
                if (!string.IsNullOrEmpty(current.Variable))
                    vars[current.Variable] = answer;
                return Resolve(nodes, current.Next, vars);

            default:
                // Nodo actual no esperaba entrada (estado obsoleto): reiniciar.
                return Resolve(nodes, flow.EntryNodeId, vars);
        }
    }

    /// <summary>
    /// Avanza desde un nodo siguiendo los nodos de mensaje (auto-continúan) hasta detenerse en un
    /// menú/input (espera entrada) o en escalado/fin. Acumula textos (con variables sustituidas).
    /// </summary>
    private static FlowStepResult Resolve(
        IReadOnlyDictionary<string, FlowNode> nodes, string? nodeId, Dictionary<string, string> vars)
    {
        var sb = new StringBuilder();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (!string.IsNullOrEmpty(nodeId))
        {
            if (!nodes.TryGetValue(nodeId, out var node) || !visited.Add(nodeId))
                break;

            switch (node.Type)
            {
                case FlowNodeType.Message:
                    Append(sb, Substitute(node.Text, vars));
                    nodeId = node.Next;
                    continue;

                case FlowNodeType.Menu:
                    Append(sb, RenderMenu(node, vars));
                    return new FlowStepResult(sb.ToString(), node.Id, false, vars);

                case FlowNodeType.Input:
                    Append(sb, Substitute(node.Text, vars));
                    return new FlowStepResult(sb.ToString(), node.Id, false, vars);

                case FlowNodeType.Escalate:
                    Append(sb, Substitute(node.Text, vars));
                    return new FlowStepResult(sb.ToString(), null, true, vars);
            }
            break;
        }

        var text = sb.Length > 0 ? sb.ToString() : "Gracias por contactarnos.";
        return new FlowStepResult(text, null, false, vars);
    }

    /// <summary>Permanece en el nodo actual (menú/input) re-mostrando un texto (p. ej. tras error).</summary>
    private static FlowStepResult Stay(FlowNode node, Dictionary<string, string> vars, string text) =>
        new(text, node.Id, false, vars);

    private static bool IsValid(FlowNode input, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return false;
        if (string.IsNullOrWhiteSpace(input.Validation)) return true;
        try { return Regex.IsMatch(answer, input.Validation, RegexOptions.None, RegexTimeout); }
        catch (RegexMatchTimeoutException) { return false; }
        catch (ArgumentException) { return true; } // regex mal formada en el flujo: no bloquear al usuario
    }

    private static string RenderMenu(FlowNode menu, IReadOnlyDictionary<string, string> vars)
    {
        var sb = new StringBuilder(Substitute(menu.Text, vars));
        foreach (var o in menu.Options ?? [])
            sb.Append('\n').Append(o.Key).Append(") ").Append(Substitute(o.Label, vars));
        return sb.ToString();
    }

    private static string Substitute(string? text, IReadOnlyDictionary<string, string> vars)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('{')) return text ?? string.Empty;
        return PlaceholderRegex.Replace(text, m =>
            vars.TryGetValue(m.Groups["key"].Value, out var v) ? v : m.Value);
    }

    private static void Append(StringBuilder sb, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (sb.Length > 0) sb.Append("\n\n");
        sb.Append(text);
    }
}
