using Mimo.Core.MultiTenancy;

namespace Mimo.UnitTests.MultiTenancy;

/// <summary>
/// Pruebas de la regla de resolución de tenant (ADR 0008). Es el punto donde se decide
/// si una petición opera sobre un tenant; la seguridad del aislamiento depende de esto.
/// </summary>
public class TenantResolverTests
{
    [Fact]
    public void Anonimo_UsaElSlugDelCliente()
    {
        var r = TenantResolver.Resolve(tokenSlug: null, clientSlug: "municipio");

        Assert.False(r.Conflict);
        Assert.Equal("municipio", r.Slug);
    }

    [Fact]
    public void Anonimo_SinSlug_NoResuelve()
    {
        var r = TenantResolver.Resolve(tokenSlug: null, clientSlug: null);

        Assert.False(r.Conflict);
        Assert.Null(r.Slug);
    }

    [Fact]
    public void Autenticado_SinSlugDeCliente_UsaElDelToken()
    {
        var r = TenantResolver.Resolve(tokenSlug: "municipio", clientSlug: null);

        Assert.False(r.Conflict);
        Assert.Equal("municipio", r.Slug);
    }

    [Fact]
    public void Autenticado_ConSlugDeClienteIgual_UsaElToken()
    {
        var r = TenantResolver.Resolve(tokenSlug: "municipio", clientSlug: "municipio");

        Assert.False(r.Conflict);
        Assert.Equal("municipio", r.Slug);
    }

    [Fact]
    public void Autenticado_ConSlugDeClienteDistinto_EsConflicto()
    {
        // Intento cross-tenant: token de A pidiendo B.
        var r = TenantResolver.Resolve(tokenSlug: "tenant-a", clientSlug: "tenant-b");

        Assert.True(r.Conflict);
        Assert.Null(r.Slug);
    }

    [Theory]
    [InlineData("Municipio", "municipio")]
    [InlineData("  municipio  ", "municipio")]
    public void NormalizaMayusculasYEspacios(string input, string esperado)
    {
        var r = TenantResolver.Resolve(tokenSlug: null, clientSlug: input);

        Assert.Equal(esperado, r.Slug);
    }

    [Fact]
    public void Autenticado_DiferenciaSoloPorMayusculas_NoEsConflicto()
    {
        var r = TenantResolver.Resolve(tokenSlug: "Municipio", clientSlug: "municipio");

        Assert.False(r.Conflict);
        Assert.Equal("municipio", r.Slug);
    }
}
