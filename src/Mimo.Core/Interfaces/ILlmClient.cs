namespace Mimo.Core.Interfaces;

/// <summary>
/// Contrato del cliente de modelo de lenguaje.
/// La implementación concreta usa la API de DeepSeek (compatible con OpenAI).
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Genera una respuesta de chat dado un historial de mensajes y un sistema de contexto.
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, IReadOnlyList<(string role, string content)> history, CancellationToken ct = default);

    /// <summary>
    /// Genera el vector de embeddings para un texto. Usado para indexar y buscar documentos.
    /// </summary>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
