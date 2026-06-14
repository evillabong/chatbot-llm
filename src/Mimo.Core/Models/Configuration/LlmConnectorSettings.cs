namespace Mimo.Core.Models.Configuration;

/// <summary>
/// Configuración específica de un conector de IA. Se persiste como JSON (jsonb) en la
/// columna settings de la tabla ai_connectors, de modo que cada proveedor pueda definir
/// sus propios parámetros sin alterar el esquema de la base de datos.
///
/// Los campos aquí cubren proveedores compatibles con la API de OpenAI (DeepSeek, OpenAI, etc.).
/// Un proveedor futuro con parámetros distintos puede extender esta clase o ignorar los no usados.
/// </summary>
public class LlmConnectorSettings
{
    /// <summary>Clave de API del proveedor.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>URL base del endpoint del proveedor (ej: https://api.deepseek.com).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Modelo usado para completions de chat.</summary>
    public string ChatModel { get; set; } = string.Empty;

    /// <summary>Modelo usado para generar embeddings.</summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>Temperatura de muestreo para las respuestas del chat.</summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>Máximo de tokens a generar por respuesta de chat.</summary>
    public int MaxTokens { get; set; } = 1024;
}
