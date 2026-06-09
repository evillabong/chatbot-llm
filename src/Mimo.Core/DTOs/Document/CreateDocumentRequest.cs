using System.ComponentModel.DataAnnotations;
using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Document;

/// <summary>
/// Datos para crear un nuevo documento de conocimiento.
/// El sistema generará el embedding automáticamente tras la creación.
/// </summary>
public record CreateDocumentRequest(
    [Required, MaxLength(300)] string Title,
    [Required] string Content,
    VisibilityLevel Visibility = VisibilityLevel.Public,
    Guid? CategoryId = null,
    Guid? RelatedRoleId = null,
    string[]? Tags = null,
    int PriorityLevel = 0
);
