using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Mcp;

/// <summary>
/// Servidor MCP externo registrado (#23). El token de autenticación NUNCA se devuelve (write-only);
/// <c>HasAuthToken</c> indica si hay uno configurado.
/// </summary>
public record McpServerResponse(
    Guid Id,
    string Name,
    string Endpoint,
    string[] AllowedTools,
    bool IsEnabled,
    int TimeoutSeconds,
    bool HasAuthToken,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Registra un servidor MCP externo.</summary>
public record CreateMcpServerRequest(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(2048)] string Endpoint,
    string? AuthToken = null,
    string[]? AllowedTools = null,
    int TimeoutSeconds = 30,
    bool IsEnabled = true
);

/// <summary>
/// Actualiza un servidor MCP. <c>AuthToken</c> vacío/nulo **conserva** el token existente (write-only);
/// uno nuevo lo reemplaza.
/// </summary>
public record UpdateMcpServerRequest(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(2048)] string Endpoint,
    string? AuthToken,
    string[]? AllowedTools,
    int TimeoutSeconds,
    bool IsEnabled
);
