namespace Mimo.Core.Common;

/// <summary>
/// Parámetros de paginación normalizados. Acota la página y el tamaño a rangos válidos
/// para evitar consultas degeneradas (páginas &lt; 1 o tamaños excesivos).
/// </summary>
public readonly record struct PaginationRequest
{
    /// <summary>Tamaño de página por defecto cuando no se especifica uno válido.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Tamaño máximo de página permitido.</summary>
    public const int MaxPageSize = 100;

    public int Page { get; }
    public int PageSize { get; }

    public PaginationRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        Page     = page < 1 ? 1 : page;
        PageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;
    }
}
