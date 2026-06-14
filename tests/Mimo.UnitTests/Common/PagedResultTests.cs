using Mimo.Core.Common;

namespace Mimo.UnitTests.Common;

/// <summary>
/// Pruebas de los metadatos de paginación y de la proyección Map.
/// </summary>
public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(40, 20, 2)]
    [InlineData(41, 20, 3)]
    public void TotalPages_SeCalculaPorTechoDelTotalEntrePagina(int total, int pageSize, int esperado)
    {
        var result = new PagedResult<int>([], total, 1, pageSize);

        Assert.Equal(esperado, result.TotalPages);
    }

    [Fact]
    public void HasPrevious_Y_HasNext_ReflejanLaPosicionDeLaPagina()
    {
        // 3 páginas en total (45 items, tamaño 20), parado en la página 2
        var middle = new PagedResult<int>([], 45, 2, 20);
        Assert.True(middle.HasPrevious);
        Assert.True(middle.HasNext);

        var first = new PagedResult<int>([], 45, 1, 20);
        Assert.False(first.HasPrevious);
        Assert.True(first.HasNext);

        var last = new PagedResult<int>([], 45, 3, 20);
        Assert.True(last.HasPrevious);
        Assert.False(last.HasNext);
    }

    [Fact]
    public void Map_ProyectaLosItems_YConservaLosMetadatos()
    {
        var origen = new PagedResult<int>([1, 2, 3], TotalCount: 50, Page: 2, PageSize: 3);

        var mapped = origen.Map(x => x * 10);

        Assert.Equal(new[] { 10, 20, 30 }, mapped.Items);
        Assert.Equal(50, mapped.TotalCount);
        Assert.Equal(2,  mapped.Page);
        Assert.Equal(3,  mapped.PageSize);
    }
}
