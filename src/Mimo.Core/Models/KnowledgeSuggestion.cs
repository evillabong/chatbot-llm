using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Sugerencia de conocimiento candidata (#22, Fase 1 — curación HITL). Vive en el esquema del tenant.
/// Un borrador (título + contenido) que un TenantAdmin revisa y, al aprobar, se publica como
/// <see cref="Document"/> (alimentando el RAG). Puede originarse en un vacío detectado
/// (<see cref="SourceSignalId"/>) o crearse manualmente; más adelante, también por el worker batch.
/// Nunca se comparte entre tenants (aislamiento por esquema, ADR 0009).
/// </summary>
public class KnowledgeSuggestion
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>Señal de vacío que motivó la sugerencia (opcional).</summary>
    public Guid? SourceSignalId { get; set; }

    public string DraftTitle { get; set; } = string.Empty;

    public string DraftContent { get; set; } = string.Empty;

    public KnowledgeSuggestionStatus Status { get; set; } = KnowledgeSuggestionStatus.Pending;

    /// <summary>Documento publicado al aprobar (null mientras esté pendiente o si se descartó).</summary>
    public Guid? PublishedDocumentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de aprobación/descarte (null mientras esté pendiente).</summary>
    public DateTime? ReviewedAt { get; set; }
}
