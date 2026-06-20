using System.Text.Json.Serialization;

namespace Mimo.Core.DTOs.Chatbot;

/// <summary>Tipo de nodo de un flujo guiado (corte 1: mensaje, menú, escalar).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FlowNodeType
{
    /// <summary>Muestra un texto y continúa automáticamente al nodo <c>Next</c>.</summary>
    Message,
    /// <summary>Muestra un texto con opciones; el usuario elige una para avanzar.</summary>
    Menu,
    /// <summary>Muestra un texto y deriva la conversación a un funcionario.</summary>
    Escalate
}

/// <summary>Opción de un nodo de menú: la clave que teclea el usuario y el nodo destino.</summary>
public record FlowOption(string Key, string Label, string Next);

/// <summary>Nodo de un flujo guiado.</summary>
public record FlowNode(
    string Id,
    FlowNodeType Type,
    string Text,
    string? Next = null,
    List<FlowOption>? Options = null
);

/// <summary>
/// Definición completa de un flujo guiado: nodo de entrada y el conjunto de nodos. Se serializa como
/// JSON en <see cref="Models.ChatbotFlow.Definition"/>.
/// </summary>
public record FlowDefinition(
    string EntryNodeId,
    List<FlowNode> Nodes
);
