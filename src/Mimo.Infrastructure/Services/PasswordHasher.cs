using System.Security.Cryptography;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Services;

/// <summary>
/// Implementación de hashing de contraseñas basada en PBKDF2 (Rfc2898DeriveBytes),
/// disponible en la biblioteca base de .NET sin dependencias adicionales.
///
/// Formato almacenado: "{iteraciones}.{salt-base64}.{hash-base64}"
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128 bits
    private const int HashSize = 32;       // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 3);
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
