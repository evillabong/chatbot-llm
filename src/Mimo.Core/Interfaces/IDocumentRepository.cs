using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a datos de documentos de conocimiento dentro del esquema de un tenant.
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> ListAsync(bool? isActive = null, Guid? categoryId = null, Guid? roleId = null, CancellationToken ct = default);

    /// <summary>
    /// Obtiene documentos visibles según el nivel de autenticación del ciudadano.
    /// El sistema aplica este filtro ANTES de entregar contexto al LLM.
    /// </summary>
    Task<IReadOnlyList<Document>> GetVisibleAsync(bool isAuthenticated, Guid? roleId, CancellationToken ct = default);

    Task<bool> TitleExistsAsync(string title, CancellationToken ct = default);
    Task<Document> AddAsync(Document document, CancellationToken ct = default);
    Task UpdateAsync(Document document, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
