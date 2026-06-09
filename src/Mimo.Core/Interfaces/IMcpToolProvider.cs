namespace Mimo.Core.Interfaces;

/// <summary>
/// Proveedor de herramientas MCP (Model Context Protocol) expuestas al LLM.
/// El LLM puede invocar estas herramientas para interactuar con el sistema.
/// </summary>
public interface IMcpToolProvider
{
    /// <summary>Busca documentos relevantes en la base de conocimiento del tenant.</summary>
    Task<string> SearchKnowledgeBaseAsync(string query, Guid tenantId, bool isAuthenticated, CancellationToken ct = default);

    /// <summary>Obtiene un documento específico por su ID.</summary>
    Task<string> GetDocumentByIdAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>Clasifica la intención del mensaje del ciudadano.</summary>
    Task<string> ClassifyIntentAsync(string message, CancellationToken ct = default);

    /// <summary>Verifica si el tenant está en horario de atención.</summary>
    Task<bool> CheckBusinessHoursAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Crea un ticket para solicitar atención humana.</summary>
    Task<string> RequestHumanAgentAsync(Guid conversationId, string reason, CancellationToken ct = default);

    /// <summary>Consulta el estado de la cola de atención de un tenant.</summary>
    Task<string> CheckQueueStatusAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
}
