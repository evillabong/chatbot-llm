using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Document;

/// <summary>Categoría documental (#17).</summary>
public record DocumentCategoryResponse(
    Guid Id,
    string Name,
    Guid? ParentCategoryId
);

/// <summary>Solicitud para crear una categoría documental.</summary>
public record CreateDocumentCategoryRequest(
    [Required, MaxLength(120)] string Name,
    Guid? ParentCategoryId = null
);
