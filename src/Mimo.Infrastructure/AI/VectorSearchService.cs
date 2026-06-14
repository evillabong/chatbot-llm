using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Mimo.Infrastructure.AI;

/// <summary>
/// Búsqueda semántica de documentos usando pgvector (cosine similarity).
/// Utiliza LINQ con CosineDistance() de Pgvector.EntityFrameworkCore en lugar de SQL crudo,
/// lo que permite que EF Core genere la consulta con el operador &lt;=&gt; de manera segura.
/// </summary>
public class VectorSearchService(TenantDbContext db, IAiGatewayService ai) : IVectorSearchService
{
    /// <summary>
    /// Genera el embedding para la consulta y recupera los documentos más similares
    /// visibles para el ciudadano (filtrado por visibilidad y rol antes de ordenar).
    /// </summary>
    public async Task<IReadOnlyList<Document>> SearchAsync(
        string query,
        Guid tenantId,
        bool isAuthenticated,
        Guid? roleId = null,
        int topK = 5,
        CancellationToken ct = default)
    {
        var rawEmbedding = await ai.GetEmbeddingAsync(tenantId, query, ct);
        var queryVector = new Vector(rawEmbedding);

        // Filtrado previo al ordenamiento: visibilidad y rol se aplican en la cláusula WHERE
        // para reducir el espacio de búsqueda antes del cálculo de distancia coseno.
        var queryable = db.Documents
            .AsNoTracking()
            .Where(d => d.IsActive && d.Embedding != null);

        if (!isAuthenticated)
            queryable = queryable.Where(d => d.Visibility == VisibilityLevel.Public);

        if (roleId.HasValue)
            queryable = queryable.Where(d => d.RelatedRoleId == null || d.RelatedRoleId == roleId.Value);

        // CosineDistance() traduce al operador <=> de pgvector.
        // Requiere el índice HNSW definido en la migración de TenantDbContext.
        return await queryable
            .OrderBy(d => d.Embedding!.CosineDistance(queryVector))
            .Take(topK)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Genera el vector de embeddings para un texto dado, a través del gateway de IA
    /// (aplica entitlements/cuotas del plan y registra el uso del tenant).
    /// </summary>
    public Task<float[]> GetEmbeddingAsync(Guid tenantId, string text, CancellationToken ct = default)
        => ai.GetEmbeddingAsync(tenantId, text, ct);
}
