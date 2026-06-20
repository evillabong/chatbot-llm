using System.Text;
using Mimo.Core.DTOs.Chatbot;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Chatbot;

/// <summary>
/// Motor determinista del chatbot por opciones (#27). Lógica pura sin BD: recorre el árbol de nodos
/// (mensaje → continúa solo; menú → espera elección; escalar → deriva a funcionario).
/// </summary>
public sealed class ChatbotFlowEngine : IChatbotFlowEngine
{
    public FlowStepResult Process(FlowDefinition flow, string? currentNodeId, string input)
    {
        var nodes = new Dictionary<string, FlowNode>(StringComparer.Ordinal);
        foreach (var n in flow.Nodes) nodes[n.Id] = n;

        // Inicio del flujo: arrancar en el nodo de entrada (se ignora la entrada del usuario).
        if (string.IsNullOrEmpty(currentNodeId))
            return Resolve(nodes, flow.EntryNodeId);

        // Si el nodo actual ya no existe o no es un menú (estado obsoleto), reiniciar el flujo.
        if (!nodes.TryGetValue(currentNodeId, out var current) || current.Type != FlowNodeType.Menu)
            return Resolve(nodes, flow.EntryNodeId);

        // Resolver la opción elegida por la clave tecleada.
        var choice = (input ?? string.Empty).Trim();
        var option = current.Options?.FirstOrDefault(o =>
            string.Equals(o.Key, choice, StringComparison.OrdinalIgnoreCase));

        if (option is null)
        {
            // Opción inválida: re-mostrar el menú actual con un aviso.
            var prompt = RenderMenu(current);
            return new FlowStepResult($"No entendí tu opción. {prompt}", current.Id, Escalate: false);
        }

        return Resolve(nodes, option.Next);
    }

    /// <summary>
    /// Avanza desde un nodo siguiendo los nodos de mensaje (auto-continúan) hasta detenerse en un
    /// menú (espera elección) o en un escalado/fin. Acumula los textos por el camino.
    /// </summary>
    private static FlowStepResult Resolve(IReadOnlyDictionary<string, FlowNode> nodes, string? nodeId)
    {
        var sb = new StringBuilder();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (!string.IsNullOrEmpty(nodeId))
        {
            if (!nodes.TryGetValue(nodeId, out var node) || !visited.Add(nodeId))
                break; // nodo inexistente o ciclo: detener

            switch (node.Type)
            {
                case FlowNodeType.Message:
                    Append(sb, node.Text);
                    nodeId = node.Next;
                    continue;

                case FlowNodeType.Menu:
                    Append(sb, RenderMenu(node));
                    return new FlowStepResult(sb.ToString(), node.Id, Escalate: false);

                case FlowNodeType.Escalate:
                    Append(sb, node.Text);
                    return new FlowStepResult(sb.ToString(), NextNodeId: null, Escalate: true);
            }
            break;
        }

        // Sin nodo válido / fin de cadena de mensajes: flujo terminado.
        var text = sb.Length > 0 ? sb.ToString() : "Gracias por contactarnos.";
        return new FlowStepResult(text, NextNodeId: null, Escalate: false);
    }

    private static string RenderMenu(FlowNode menu)
    {
        var sb = new StringBuilder(menu.Text);
        foreach (var o in menu.Options ?? [])
            sb.Append('\n').Append(o.Key).Append(") ").Append(o.Label);
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (sb.Length > 0) sb.Append("\n\n");
        sb.Append(text);
    }
}
