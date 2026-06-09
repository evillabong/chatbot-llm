using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.AI;

/// <summary>
/// Búsqueda semántica de documentos usando pgvector (cosine similarity).
/// El sistema filtra visibilidad y tenant ANTES de ordenar por similitud.
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly MimoDbContext _db;
    private readonly ILlmClient _llm;

    public VectorSearchService(MimoDbContext db, ILlmClient llm)
    {
        _db  = db;
        _llm = llm;
    }

    /// <summary>
    /// Genera el embedding para la consulta y recupera los documentos más similares
    /// visibles para el ciudadano (filtrado por visibilidad y rol).
    /// </summary>
    public async Task<IReadOnlyList<Document>> SearchAsync(
        string query,
        Guid tenantId,
        bool isAuthenticated,
        Guid? roleId = null,
        int topK = 5,
        CancellationToken ct = default)
    {
        var queryEmbedding = await _llm.GetEmbeddingAsync(query, ct);

        // Construir el string del vector para la consulta pgvector
        var vectorLiteral = $"[{string.Join(",", queryEmbedding)}]";

        // Filtro de visibilidad: ciudadanos no autenticados solo ven documentos públicos.
        // La restricción se aplica en SQL antes de ordenar por similitud.
        var visibilityFilter = isAuthenticated
            ? ""
            : "AND d.visibility = 'Public'";

        var roleFilter = roleId.HasValue
            ? $"AND (d.related_role_id IS NULL OR d.related_role_id = '{roleId}')"
            : "";

        // Búsqueda por cosine distance (<=>). Menor distancia = mayor similitud.
        // Requiere índice HNSW creado por create_tenant_schema().
        var sql = $"""
            SELECT * FROM documents d
            WHERE d.is_active = true
              AND d.embedding IS NOT NULL
              {visibilityFilter}
              {roleFilter}
            ORDER BY d.embedding <=> '{vectorLiteral}'::vector
            LIMIT {topK}
            """;

        return await _db.Documents
            .FromSqlRaw(sql)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Genera el vector de embeddings para un texto dado.
    /// Delegado al cliente LLM configurado.
    /// </summary>
    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        => _llm.GetEmbeddingAsync(text, ct);
}
