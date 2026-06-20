using Mimo.Core.DTOs.Chatbot;

namespace Mimo.Core.Interfaces;

/// <summary>Resultado de un paso del motor de flujos guiados.</summary>
/// <param name="Text">Texto a mostrar al ciudadano.</param>
/// <param name="NextNodeId">Nodo en el que queda la conversación (null = flujo terminado).</param>
/// <param name="Escalate">True si el flujo pide derivar a un funcionario.</param>
/// <param name="Variables">Estado de variables capturadas tras el paso (a persistir).</param>
public record FlowStepResult(
    string Text,
    string? NextNodeId,
    bool Escalate,
    IReadOnlyDictionary<string, string> Variables);

/// <summary>
/// Motor determinista del chatbot por opciones (#27). Lógica pura, sin estado ni BD: dado el flujo,
/// el nodo actual, las variables capturadas y la entrada del usuario, calcula el siguiente paso. La
/// persistencia (nodo actual, variables, mensajes) la maneja el orquestador.
/// </summary>
public interface IChatbotFlowEngine
{
    /// <summary>
    /// Procesa un turno. Si <paramref name="currentNodeId"/> es null, inicia el flujo en el nodo de
    /// entrada (ignora <paramref name="input"/>); si no, interpreta <paramref name="input"/> como la
    /// opción elegida (menú) o el dato capturado (input). <paramref name="variables"/> es el estado
    /// previo de variables de la conversación.
    /// </summary>
    FlowStepResult Process(
        FlowDefinition flow,
        string? currentNodeId,
        IReadOnlyDictionary<string, string> variables,
        string input);
}
