using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Búsqueda semántica de documentos usando embeddings en pgvector.
/// El sistema aplica los filtros ANTES de llamar al LLM.
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Busca documentos relevantes para una consulta dentro de un tenant.
    /// Filtra por visibilidad según si el ciudadano está autenticado.
    /// </summary>
    Task<IReadOnlyList<Document>> SearchAsync(
        string query,
        Guid tenantId,
        bool isAuthenticated,
        Guid? roleId = null,
        int topK = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Genera el vector de embeddings para un texto, atribuyendo el uso al tenant indicado.
    /// </summary>
    Task<float[]> GetEmbeddingAsync(Guid tenantId, string text, CancellationToken ct = default);
}
