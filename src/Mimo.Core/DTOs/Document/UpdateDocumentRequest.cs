using System.ComponentModel.DataAnnotations;
using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Document;

/// <summary>
/// Datos actualizables de un documento. Si cambia el contenido se regenera el embedding.
/// </summary>
public record UpdateDocumentRequest(
    [Required, MaxLength(300)] string Title,
    [Required] string Content,
    VisibilityLevel Visibility,
    Guid? CategoryId,
    Guid? RelatedRoleId,
    string[]? Tags,
    int PriorityLevel,
    bool IsActive
);
