namespace Mimo.Core.Models;

/// <summary>
/// Señal de recuperación semántica por consulta (#22, mejora continua — Fase 1). Vive en el esquema
/// del tenant. Captura, por cada consulta atendida por IA, la mejor similitud de recuperación y si la
/// consulta quedó marcada como <c>knowledge_gap</c> (vacío de conocimiento). Es la materia prima para
/// detectar vacíos y, más adelante, generar sugerencias de conocimiento curables por un humano.
/// Nunca se comparte entre tenants (aislamiento por esquema, ADR 0009).
/// </summary>
public class KnowledgeQuerySignal
{
    public Guid Id { get; set; }

    /// <summary>Tenant dueño de la señal (defensa en profundidad; el esquema ya aísla).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Conversación en la que se produjo la consulta.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>Mensaje del ciudadano que originó la consulta (opcional).</summary>
    public Guid? MessageId { get; set; }

    /// <summary>Texto de la consulta (recortado) que se usó para la búsqueda semántica.</summary>
    public string QueryText { get; set; } = string.Empty;

    /// <summary>Mejor similitud de recuperación (1 - distancia coseno). Null si no hubo coincidencias.</summary>
    public double? TopSimilarity { get; set; }

    /// <summary>Número de documentos recuperados por encima del filtro de visibilidad/rol.</summary>
    public int MatchCount { get; set; }

    /// <summary>True si la recuperación quedó por debajo del umbral o no hubo coincidencias.</summary>
    public bool KnowledgeGap { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
