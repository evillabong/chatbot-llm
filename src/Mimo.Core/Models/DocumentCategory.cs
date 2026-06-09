namespace Mimo.Core.Models;

/// <summary>
/// Categoría documental con soporte para jerarquía (categorías y subcategorías).
/// </summary>
public class DocumentCategory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Categoría padre para estructura jerárquica. Null indica categoría raíz.</summary>
    public Guid? ParentCategoryId { get; set; }
    public DocumentCategory? ParentCategory { get; set; }

    public ICollection<DocumentCategory> SubCategories { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
}
