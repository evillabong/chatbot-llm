namespace Mimo.Core.Common;

/// <summary>
/// Resultado paginado genérico y reutilizable para cualquier listado.
/// Transporta los elementos de la página actual junto con los metadatos de paginación.
/// </summary>
/// <typeparam name="T">Tipo de los elementos de la página.</typeparam>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>Número total de páginas según el tamaño de página.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Indica si existe una página anterior.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>Indica si existe una página siguiente.</summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Proyecta los elementos a otro tipo conservando los metadatos de paginación.
    /// Útil para mapear entidades a DTOs sin perder la información de la página.
    /// </summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new([.. Items.Select(selector)], TotalCount, Page, PageSize);

    /// <summary>Página vacía (sin elementos).</summary>
    public static PagedResult<T> Empty(int page, int pageSize) =>
        new([], 0, page, pageSize);
}
