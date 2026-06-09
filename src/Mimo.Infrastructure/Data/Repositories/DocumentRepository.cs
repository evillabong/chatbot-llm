using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de documentos de conocimiento de un tenant.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly MimoDbContext _db;

    public DocumentRepository(MimoDbContext db) => _db = db;

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Document>> ListAsync(
        bool? isActive = null,
        Guid? categoryId = null,
        Guid? roleId = null,
        CancellationToken ct = default)
    {
        var q = _db.Documents.AsQueryable();

        if (isActive.HasValue)   q = q.Where(d => d.IsActive == isActive.Value);
        if (categoryId.HasValue) q = q.Where(d => d.CategoryId == categoryId);
        if (roleId.HasValue)     q = q.Where(d => d.RelatedRoleId == roleId);

        return await q.OrderByDescending(d => d.PriorityLevel)
                      .ThenBy(d => d.Title)
                      .ToListAsync(ct);
    }

    /// <summary>
    /// Filtra documentos según visibilidad.
    /// Ciudadanos no autenticados solo ven documentos públicos.
    /// El sistema aplica este filtro ANTES de enviar contexto al LLM.
    /// </summary>
    public async Task<IReadOnlyList<Document>> GetVisibleAsync(
        bool isAuthenticated, Guid? roleId, CancellationToken ct = default)
    {
        var q = _db.Documents.Where(d => d.IsActive);

        if (!isAuthenticated)
            q = q.Where(d => d.Visibility == VisibilityLevel.Public);

        if (roleId.HasValue)
            q = q.Where(d => d.RelatedRoleId == null || d.RelatedRoleId == roleId);

        return await q.OrderByDescending(d => d.PriorityLevel).ToListAsync(ct);
    }

    public Task<bool> TitleExistsAsync(string title, CancellationToken ct = default)
        => _db.Documents.AnyAsync(d => d.Title == title, ct);

    public async Task<Document> AddAsync(Document document, CancellationToken ct = default)
    {
        _db.Documents.Add(document);
        await _db.SaveChangesAsync(ct);
        return document;
    }

    public Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        _db.Documents.Update(document);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
