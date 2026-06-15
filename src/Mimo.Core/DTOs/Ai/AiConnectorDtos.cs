using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Ai;

/// <summary>
/// Representación de un conector de IA para la administración. NO expone la API key:
/// solo indica si está configurada (HasApiKey).
/// </summary>
public record AiConnectorResponse(
    Guid Id,
    string Provider,
    string DisplayName,
    bool IsActive,
    string BaseUrl,
    string ChatModel,
    string EmbeddingModel,
    float Temperature,
    int MaxTokens,
    bool HasApiKey,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Configuración del proveedor enviada por el administrador (incluye la API key en claro al escribir).</summary>
public record AiConnectorSettingsDto(
    [Required] string ApiKey,
    [Required, Url] string BaseUrl,
    [Required] string ChatModel,
    [Required] string EmbeddingModel,
    [Range(0, 2)] float Temperature = 0.7f,
    [Range(1, 32000)] int MaxTokens = 1024
);

/// <summary>Datos para crear un conector de IA.</summary>
public record CreateAiConnectorRequest(
    [Required, MaxLength(50)] string Provider,
    [Required, MaxLength(100)] string DisplayName,
    [Required] AiConnectorSettingsDto Settings
);

/// <summary>Datos para actualizar un conector de IA (el proveedor es inmutable).</summary>
public record UpdateAiConnectorRequest(
    [Required, MaxLength(100)] string DisplayName,
    [Required] AiConnectorSettingsDto Settings
);
