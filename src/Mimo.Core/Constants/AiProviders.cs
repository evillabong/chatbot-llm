namespace Mimo.Core.Constants;

/// <summary>
/// Claves de los proveedores de IA soportados. El valor se almacena en
/// AiConnector.Provider y la fábrica de clientes lo usa para instanciar la
/// implementación concreta de ILlmClient.
/// </summary>
public static class AiProviders
{
    /// <summary>DeepSeek (API compatible con OpenAI).</summary>
    public const string DeepSeek = "deepseek";
}
