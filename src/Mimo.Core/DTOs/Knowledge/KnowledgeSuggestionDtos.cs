using System.ComponentModel.DataAnnotations;
using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Knowledge;

/// <summary>Sugerencia de conocimiento (#22, Fase 1 — curación HITL).</summary>
public record KnowledgeSuggestionResponse(
    Guid Id,
    Guid? SourceSignalId,
    string DraftTitle,
    string DraftContent,
    KnowledgeSuggestionStatus Status,
    Guid? PublishedDocumentId,
    DateTime CreatedAt,
    DateTime? ReviewedAt
);

/// <summary>Crea una sugerencia de conocimiento candidata (manual o desde un vacío detectado).</summary>
public record CreateKnowledgeSuggestionRequest(
    [Required, MaxLength(200)] string DraftTitle,
    [Required] string DraftContent,
    Guid? SourceSignalId = null
);
