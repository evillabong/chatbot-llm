using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Documento de conocimiento usado como contexto para el bot de IA.
/// El sistema filtra los documentos visibles antes de enviarlos al LLM.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>Visibilidad: Public (sin autenticación) o Private (solo ciudadanos autenticados).</summary>
    public VisibilityLevel Visibility { get; set; } = VisibilityLevel.Public;

    public Guid? CategoryId { get; set; }
    public DocumentCategory? Category { get; set; }

    /// <summary>Rol al que está vinculado el conocimiento de este documento.</summary>
    public Guid? RelatedRoleId { get; set; }

    /// <summary>Palabras clave para filtrado adicional.</summary>
    public string[] Tags { get; set; } = [];

    /// <summary>Prioridad para ordenar resultados de búsqueda semántica.</summary>
    public int PriorityLevel { get; set; } = 0;

    /// <summary>Vector de embeddings para búsqueda semántica con pgvector. Dimensión: 1536.</summary>
    public float[]? Embedding { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Funcionario que cargó el documento.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
