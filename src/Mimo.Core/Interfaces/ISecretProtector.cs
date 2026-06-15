namespace Mimo.Core.Interfaces;

/// <summary>
/// Cifra y descifra secretos en reposo (p. ej. la API key de un conector de IA).
/// Una cadena vacía o nula se trata como "sin secreto" y se devuelve vacía.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Cifra un valor en texto plano para almacenarlo.</summary>
    string Protect(string? plaintext);

    /// <summary>Descifra un valor previamente cifrado con <see cref="Protect"/>.</summary>
    string Unprotect(string? protectedValue);
}
