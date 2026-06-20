using Mimo.Core.DTOs.Chatbot;

namespace Mimo.Core.Interfaces;

/// <summary>Resultado de ejecutar un nodo ApiCall.</summary>
/// <param name="Success">True si la API respondió 2xx.</param>
/// <param name="CapturedValue">Cuerpo de la respuesta (recortado) para guardar en una variable, si aplica.</param>
public record ApiCallResult(bool Success, string? CapturedValue);

/// <summary>
/// Ejecuta la llamada HTTP de un nodo ApiCall del chatbot por opciones (#27 corte 3), aplicando los
/// controles de seguridad (allow-list de hosts, bloqueo de IPs internas/metadata, timeout, tamaño
/// máximo, sin redirecciones). Vive fuera del motor (que es puro) porque hace I/O.
/// </summary>
public interface IChatbotApiCaller
{
    Task<ApiCallResult> CallAsync(
        FlowNode apiNode, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default);
}
