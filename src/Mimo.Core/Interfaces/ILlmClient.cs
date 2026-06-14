using Mimo.Core.Models.Ai;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Contrato de un conector concreto de modelo de lenguaje (ej: DeepSeek).
/// Devuelve también el uso de tokens para que el gateway pueda medir el consumo.
///
/// Los consumidores de dominio NO usan esta interfaz directamente, sino
/// <see cref="IAiGatewayService"/>, que aplica entitlements y cuotas por plan.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Genera una respuesta de chat dado un historial de mensajes y un prompt de sistema.
    /// </summary>
    Task<LlmChatResult> ChatAsync(string systemPrompt, IReadOnlyList<(string role, string content)> history, CancellationToken ct = default);

    /// <summary>
    /// Genera el vector de embeddings para un texto. Usado para indexar y buscar documentos.
    /// </summary>
    Task<LlmEmbeddingResult> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
