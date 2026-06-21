namespace Mimo.Core.Models;

/// <summary>
/// Servidor MCP (Model Context Protocol) externo registrado por una organización (#23). Vive en el
/// esquema del tenant. Declara un endpoint de herramientas de terceros que el gateway de IA podrá
/// ofrecer al modelo y **mediar** su invocación (auditoría y límites) en un corte posterior. Por ahora
/// es el **catálogo**: nada se invoca sin estar registrado, habilitado y con la herramienta en la
/// allowlist (deny-by-default). El token de autenticación se guarda **cifrado** (ADR 0010) y es
/// write-only. Aislado por organización; nunca cross-tenant.
/// </summary>
public class McpServer
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>Nombre legible del servidor (único por tenant).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Endpoint del servidor MCP (URL absoluta http/https).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Token de autenticación cifrado en reposo (null = sin autenticación). Write-only.</summary>
    public string? AuthToken { get; set; }

    /// <summary>Allowlist de herramientas habilitadas (deny-by-default: vacío = ninguna).</summary>
    public string[] AllowedTools { get; set; } = [];

    /// <summary>Si está deshabilitado, el gateway no lo ofrece al modelo.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Timeout de invocación en segundos.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
