namespace Mimo.Core.Interfaces;

/// <summary>
/// Hashing y verificación de contraseñas.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Genera el hash de una contraseña en texto plano.</summary>
    string Hash(string password);

    /// <summary>Verifica que una contraseña en texto plano coincida con un hash previamente generado.</summary>
    bool Verify(string password, string passwordHash);
}
