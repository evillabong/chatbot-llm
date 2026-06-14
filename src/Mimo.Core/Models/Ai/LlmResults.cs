namespace Mimo.Core.Models.Ai;

/// <summary>
/// Consumo de tokens reportado por el proveedor de IA. Si el proveedor no lo reporta,
/// se usa <see cref="None"/> (ceros) y la cuota recae sobre el conteo de solicitudes.
/// </summary>
public record LlmUsage(int PromptTokens, int CompletionTokens, int TotalTokens)
{
    public static readonly LlmUsage None = new(0, 0, 0);
}

/// <summary>Resultado de una completion de chat: contenido y uso de tokens.</summary>
public record LlmChatResult(string Content, LlmUsage Usage);

/// <summary>Resultado de una generación de embedding: vector y uso de tokens.</summary>
public record LlmEmbeddingResult(float[] Embedding, LlmUsage Usage);
