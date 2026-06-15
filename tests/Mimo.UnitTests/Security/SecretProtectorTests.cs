using Microsoft.AspNetCore.DataProtection;
using Mimo.Core.Interfaces;
using Mimo.Infrastructure.Security;

namespace Mimo.UnitTests.Security;

/// <summary>
/// Pruebas del cifrado de secretos en reposo (API keys de conectores de IA).
/// Incluye el escenario clave (ADR 0010): cifrar en una "instancia" y descifrar en otra
/// compartiendo el anillo de llaves (simula Admin.Api cifrando y Mimo.Api descifrando).
/// </summary>
public class SecretProtectorTests
{
    private static string NewKeysDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mimo-dp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static ISecretProtector NewProtector(string keysDir)
    {
        var provider = DataProtectionProvider.Create(
            new DirectoryInfo(keysDir),
            builder => builder.SetApplicationName("MIMO"));
        return new DataProtectionSecretProtector(provider);
    }

    [Fact]
    public void Protect_LuegoUnprotect_DevuelveElOriginal()
    {
        var p = NewProtector(NewKeysDir());
        const string secret = "sk-deepseek-abc123";

        var cifrado = p.Protect(secret);

        Assert.NotEqual(secret, cifrado);          // realmente cifra
        Assert.Equal(secret, p.Unprotect(cifrado)); // roundtrip
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValorVacio_SeTrataComoSinSecreto(string? input)
    {
        var p = NewProtector(NewKeysDir());

        Assert.Equal(string.Empty, p.Protect(input));
        Assert.Equal(string.Empty, p.Unprotect(input));
    }

    [Fact]
    public void CifradoEnUnaInstancia_SeDescifraEnOtra_ConElMismoAnillo()
    {
        // Dos procesos distintos (Admin.Api cifra, Mimo.Api descifra) compartiendo llaves.
        var keys   = NewKeysDir();
        var writer = NewProtector(keys);
        var reader = NewProtector(keys);

        const string secret = "clave-compartida-entre-apis";
        var cifrado = writer.Protect(secret);

        Assert.Equal(secret, reader.Unprotect(cifrado));
    }
}
