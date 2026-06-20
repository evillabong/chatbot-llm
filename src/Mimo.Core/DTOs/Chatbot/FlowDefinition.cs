using System.Text.Json.Serialization;

namespace Mimo.Core.DTOs.Chatbot;

/// <summary>Tipo de nodo de un flujo guiado.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FlowNodeType
{
    /// <summary>Muestra un texto y continúa automáticamente al nodo <c>Next</c>.</summary>
    Message,
    /// <summary>Muestra un texto con opciones; el usuario elige una para avanzar.</summary>
    Menu,
    /// <summary>Muestra un texto y deriva la conversación a un funcionario.</summary>
    Escalate,
    /// <summary>Pide un dato al usuario, lo valida y lo guarda en una variable (corte 2).</summary>
    Input,
    /// <summary>Llama a una API externa y bifurca según el resultado (corte 3); el usuario no interactúa.</summary>
    ApiCall
}

/// <summary>Opción de un nodo de menú: la clave que teclea el usuario y el nodo destino.</summary>
public record FlowOption(string Key, string Label, string Next);

/// <summary>Nodo de un flujo guiado.</summary>
/// <param name="Variable">Nodo Input: nombre de la variable donde se guarda el dato capturado.</param>
/// <param name="Validation">Nodo Input: regex opcional que debe cumplir el dato.</param>
/// <param name="ValidationError">Nodo Input: mensaje cuando el dato no cumple la validación.</param>
/// <param name="Method">Nodo ApiCall: método HTTP (GET/POST). Por defecto GET.</param>
/// <param name="Url">Nodo ApiCall: URL (admite <c>{variable}</c>). Debe ser un host permitido.</param>
/// <param name="Body">Nodo ApiCall: cuerpo JSON para POST (admite <c>{variable}</c>).</param>
/// <param name="CaptureVariable">Nodo ApiCall: variable donde guardar el cuerpo de la respuesta.</param>
/// <param name="SuccessNext">Nodo ApiCall: nodo destino si la llamada tuvo éxito (2xx).</param>
/// <param name="FailureNext">Nodo ApiCall: nodo destino si la llamada falló.</param>
public record FlowNode(
    string Id,
    FlowNodeType Type,
    string Text,
    string? Next = null,
    List<FlowOption>? Options = null,
    string? Variable = null,
    string? Validation = null,
    string? ValidationError = null,
    string? Method = null,
    string? Url = null,
    string? Body = null,
    string? CaptureVariable = null,
    string? SuccessNext = null,
    string? FailureNext = null
);

/// <summary>
/// Definición completa de un flujo guiado: nodo de entrada y el conjunto de nodos. Se serializa como
/// JSON en <see cref="Models.ChatbotFlow.Definition"/>.
/// </summary>
public record FlowDefinition(
    string EntryNodeId,
    List<FlowNode> Nodes
);
