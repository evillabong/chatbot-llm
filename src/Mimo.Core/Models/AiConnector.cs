using Mimo.Core.Models.Configuration;

namespace Mimo.Core.Models;

/// <summary>
/// Configuración de un conector de IA a nivel de plataforma (esquema public).
///
/// La plataforma puede tener varios conectores registrados (DeepSeek, OpenAI, etc.),
/// pero solo uno activo a la vez. El conector activo es el que usa el bot para chat
/// y embeddings. Cambiar de proveedor es, por tanto, un cambio de configuración en BD,
/// no un cambio de código: las implementaciones concretas se resuelven por Provider.
/// </summary>
public class AiConnector
{
    public Guid Id { get; set; }

    /// <summary>
    /// Clave del proveedor que determina qué implementación de ILlmClient se instancia
    /// (ver Mimo.Core.Constants.AiProviders). Único por plataforma.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo para mostrar en la administración.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Indica si este es el conector activo de la plataforma.</summary>
    public bool IsActive { get; set; }

    /// <summary>Configuración específica del proveedor, persistida como JSON (jsonb).</summary>
    public LlmConnectorSettings Settings { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
