namespace Mimo.Core.Models;

/// <summary>
/// Flujo guiado del chatbot por catálogo de opciones (#27): modo de respuesta determinista, sin IA.
/// Cada tenant puede tener flujos; el activo se usa para conducir la conversación por menús/árbol de
/// opciones. La definición (nodos) se guarda como JSON. Vive en el esquema del tenant.
/// </summary>
public class ChatbotFlow
{
    public Guid Id { get; set; }

    /// <summary>Nombre descriptivo del flujo.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Solo un flujo activo por tenant conduce las conversaciones en modo opciones.</summary>
    public bool IsActive { get; set; }

    /// <summary>Definición del flujo (JSON de <see cref="DTOs.Chatbot.FlowDefinition"/>): nodos y transiciones.</summary>
    public string Definition { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
