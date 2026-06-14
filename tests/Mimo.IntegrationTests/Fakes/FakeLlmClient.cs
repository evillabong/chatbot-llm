using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Models.Ai;

namespace Mimo.IntegrationTests.Fakes;

/// <summary>
/// Cliente LLM falso: devuelve respuestas fijas con un uso de tokens conocido,
/// para verificar la lógica del gateway sin llamar a un proveedor real.
/// </summary>
public sealed class FakeLlmClient : ILlmClient
{
    public static readonly LlmUsage FixedUsage = new(PromptTokens: 10, CompletionTokens: 20, TotalTokens: 30);

    public Task<LlmChatResult> ChatAsync(
        string systemPrompt, IReadOnlyList<(string role, string content)> history, CancellationToken ct = default)
        => Task.FromResult(new LlmChatResult("respuesta-fija", FixedUsage));

    public Task<LlmEmbeddingResult> GetEmbeddingAsync(string text, CancellationToken ct = default)
        => Task.FromResult(new LlmEmbeddingResult([0.1f, 0.2f, 0.3f], FixedUsage));
}

/// <summary>
/// Fábrica falsa que siempre devuelve <see cref="FakeLlmClient"/> y recuerda el último
/// conector solicitado, para aislar al gateway del proveedor concreto.
/// </summary>
public sealed class FakeLlmClientFactory : ILlmClientFactory
{
    public AiConnector? LastConnector { get; private set; }

    public ILlmClient Create(AiConnector connector)
    {
        LastConnector = connector;
        return new FakeLlmClient();
    }
}
