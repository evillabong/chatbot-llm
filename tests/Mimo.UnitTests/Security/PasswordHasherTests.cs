using Mimo.Infrastructure.Services;

namespace Mimo.UnitTests.Security;

/// <summary>
/// Pruebas del hashing de contraseñas (PBKDF2). Verifica el roundtrip, el rechazo de
/// contraseñas incorrectas y la robustez ante hashes malformados.
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_NoDevuelveLaContrasenaEnTextoPlano()
    {
        var hash = _hasher.Hash("SuperSecreta123");

        Assert.NotEqual("SuperSecreta123", hash);
        Assert.Contains('.', hash); // formato iteraciones.salt.hash
    }

    [Fact]
    public void Verify_DevuelveTrue_ConLaContrasenaCorrecta()
    {
        var hash = _hasher.Hash("SuperSecreta123");

        Assert.True(_hasher.Verify("SuperSecreta123", hash));
    }

    [Fact]
    public void Verify_DevuelveFalse_ConLaContrasenaIncorrecta()
    {
        var hash = _hasher.Hash("SuperSecreta123");

        Assert.False(_hasher.Verify("otraClave", hash));
    }

    [Fact]
    public void Hash_GeneraSaltsDistintos_ParaLaMismaContrasena()
    {
        var a = _hasher.Hash("misma");
        var b = _hasher.Hash("misma");

        Assert.NotEqual(a, b);               // salt aleatorio por hash
        Assert.True(_hasher.Verify("misma", a));
        Assert.True(_hasher.Verify("misma", b));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sin-separadores")]
    [InlineData("solo.dos")]
    public void Verify_DevuelveFalse_ConHashMalformado(string malformed)
    {
        Assert.False(_hasher.Verify("loquesea", malformed));
    }
}
