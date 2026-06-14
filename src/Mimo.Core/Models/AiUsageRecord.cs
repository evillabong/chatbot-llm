using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Registro de uso de IA por tenant (esquema public, append-only).
/// Cada llamada exitosa al LLM genera un registro; el SuperAdmin agrega estos datos
/// para estadísticas y el gateway los suma para verificar cuotas del periodo.
/// </summary>
public class AiUsageRecord
{
    public Guid Id { get; set; }

    /// <summary>Tenant que originó la consulta.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Proveedor usado (ver AiProviders).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Modelo concreto invocado.</summary>
    public string Model { get; set; } = string.Empty;

    public AiOperation Operation { get; set; }

    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
