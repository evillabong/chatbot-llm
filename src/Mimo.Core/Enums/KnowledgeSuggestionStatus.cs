namespace Mimo.Core.Enums;

/// <summary>
/// Estado de una sugerencia de conocimiento (#22, Fase 1 — curación HITL).
/// </summary>
public enum KnowledgeSuggestionStatus
{
    /// <summary>Pendiente de revisión por un TenantAdmin.</summary>
    Pending = 0,

    /// <summary>Aprobada: se publicó como <see cref="Models.Document"/>.</summary>
    Approved = 1,

    /// <summary>Descartada por el revisor.</summary>
    Discarded = 2
}
