using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Document;

/// <summary>
/// Representación de un documento para las respuestas de la API.
/// El embedding no se expone en la respuesta.
/// </summary>
public record DocumentResponse(
    Guid Id,
    string Title,
    string Content,
    VisibilityLevel Visibility,
    Guid? CategoryId,
    Guid? RelatedRoleId,
    string[] Tags,
    int PriorityLevel,
    bool IsActive,
    bool HasEmbedding,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
