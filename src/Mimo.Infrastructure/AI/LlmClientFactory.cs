using Microsoft.Extensions.Logging;
using Mimo.Core.Constants;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;

namespace Mimo.Infrastructure.AI;

/// <summary>
/// Construye el cliente LLM concreto a partir de un conector de IA.
///
/// Para añadir un nuevo proveedor de IA basta con:
///   1. Implementar ILlmClient para ese proveedor.
///   2. Registrar su clave en Mimo.Core.Constants.AiProviders.
///   3. Añadir el caso correspondiente en el switch de Create.
/// La selección del conector y el control de uso/cuotas los hace AiGatewayService.
///
/// La API key del conector está cifrada en reposo (ver ADR 0010); aquí se descifra
/// justo antes de construir el cliente concreto.
/// </summary>
public class LlmClientFactory(
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory,
    ISecretProtector protector) : ILlmClientFactory
{
    /// <summary>Nombre del HttpClient con nombre usado por todos los conectores de IA.</summary>
    public const string HttpClientName = "llm";

    public ILlmClient Create(AiConnector connector)
    {
        var http = httpClientFactory.CreateClient(HttpClientName);

        // Descifrar la API key sin mutar el conector original (puede venir AsNoTracking).
        var s = connector.Settings;
        var settings = new LlmConnectorSettings
        {
            ApiKey         = protector.Unprotect(s.ApiKey),
            BaseUrl        = s.BaseUrl,
            ChatModel      = s.ChatModel,
            EmbeddingModel = s.EmbeddingModel,
            Temperature    = s.Temperature,
            MaxTokens      = s.MaxTokens
        };

        return connector.Provider switch
        {
            AiProviders.DeepSeek => new DeepSeekClient(http, settings, loggerFactory.CreateLogger<DeepSeekClient>()),
            _ => throw new NotSupportedException($"Proveedor de IA no soportado: '{connector.Provider}'.")
        };
    }
}
