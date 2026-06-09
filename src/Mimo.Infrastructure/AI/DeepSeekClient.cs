using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.AI;

/// <summary>
/// Cliente HTTP para la API de DeepSeek (compatible con OpenAI).
/// Maneja completions de chat y generación de embeddings.
/// Configuración esperada en appsettings:
///   DeepSeek:ApiKey
///   DeepSeek:BaseUrl        (default: https://api.deepseek.com)
///   DeepSeek:ChatModel      (default: deepseek-chat)
///   DeepSeek:EmbeddingModel (default: deepseek-embedding)
/// </summary>
public class DeepSeekClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _chatModel;
    private readonly string _embeddingModel;
    private readonly ILogger<DeepSeekClient> _logger;

    public DeepSeekClient(HttpClient http, IConfiguration config, ILogger<DeepSeekClient> logger)
    {
        _http           = http;
        _chatModel      = config["DeepSeek:ChatModel"]      ?? "deepseek-chat";
        _embeddingModel = config["DeepSeek:EmbeddingModel"] ?? "deepseek-embedding";
        _logger         = logger;
    }

    /// <summary>
    /// Genera una respuesta del bot dado un prompt de sistema y el historial de la conversación.
    /// </summary>
    public async Task<string> ChatAsync(
        string systemPrompt,
        IReadOnlyList<(string role, string content)> history,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new("system", systemPrompt)
        };
        messages.AddRange(history.Select(h => new ChatMessage(h.role, h.content)));

        var request = new ChatRequest(_chatModel, messages, Temperature: 0.7f, MaxTokens: 1024);

        var response = await _http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta vacía de DeepSeek.");

        return result.Choices[0].Message.Content;
    }

    /// <summary>
    /// Genera el vector de embeddings para un texto.
    /// Dimensión: configurable en appsettings (default 1536 para compatibilidad con pgvector).
    /// </summary>
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var request = new EmbeddingRequest(_embeddingModel, text);

        var response = await _http.PostAsJsonAsync("/v1/embeddings", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta de embedding vacía.");

        return result.Data[0].Embedding;
    }

    // ── Modelos internos de la API ────────────────────────────────────────────

    private record ChatRequest(
        string Model,
        List<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] float Temperature = 0.7f,
        [property: JsonPropertyName("max_tokens")]  int MaxTokens = 1024
    );

    private record ChatMessage(string Role, string Content);

    private record ChatResponse(
        [property: JsonPropertyName("choices")] List<Choice> Choices
    );

    private record Choice(
        [property: JsonPropertyName("message")] ChatMessage Message
    );

    private record EmbeddingRequest(
        string Model,
        string Input
    );

    private record EmbeddingResponse(
        [property: JsonPropertyName("data")] List<EmbeddingData> Data
    );

    private record EmbeddingData(
        [property: JsonPropertyName("embedding")] float[] Embedding
    );
}
