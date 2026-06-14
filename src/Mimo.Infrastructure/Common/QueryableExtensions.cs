using Microsoft.EntityFrameworkCore;
using Mimo.Core.Common;

namespace Mimo.Infrastructure.Common;

/// <summary>
/// Extensiones reutilizables para consultas LINQ/EF Core.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Pagina cualquier <see cref="IQueryable{T}"/> y materializa la página solicitada
    /// junto con el conteo total, devolviendo un <see cref="PagedResult{T}"/>.
    /// Centraliza la lógica de paginación para evitar repetir Count/Skip/Take en cada repositorio.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest pagination,
        CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, total, pagination.Page, pagination.PageSize);
    }

    /// <summary>
    /// Sobrecarga que acepta página y tamaño sueltos; los normaliza vía <see cref="PaginationRequest"/>.
    /// </summary>
    public static Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => query.ToPagedResultAsync(new PaginationRequest(page, pageSize), ct);
}
