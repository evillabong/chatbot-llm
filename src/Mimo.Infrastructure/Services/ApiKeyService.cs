using System.Security.Cryptography;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Services;

/// <summary>
/// Genera claves de API con el formato "mk_{base64url(32 bytes)}" y las hashea con SHA-256.
/// Solo se persiste el hash y un prefijo; la clave en claro se entrega una única vez.
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    private const string KeyPrefix = "mk_";
    private const int PrefixDisplayLength = 12; // p. ej. "mk_AbC123dE4"

    public GeneratedApiKey Generate()
    {
        var bytes  = RandomNumberGenerator.GetBytes(32);
        var random = Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');   // base64url sin padding
        var plain  = KeyPrefix + random;

        var prefix = plain.Length > PrefixDisplayLength ? plain[..PrefixDisplayLength] : plain;
        return new GeneratedApiKey(plain, Hash(plain), prefix);
    }

    public string Hash(string plainKey)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plainKey));
        return Convert.ToHexString(hash);   // hex en mayúsculas
    }
}
