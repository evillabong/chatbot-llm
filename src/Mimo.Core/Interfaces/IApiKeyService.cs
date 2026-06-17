namespace Mimo.Core.Interfaces;

/// <summary>Clave de API recién generada: el valor en claro (se muestra una vez), su hash y su prefijo.</summary>
public record GeneratedApiKey(string PlainKey, string Hash, string Prefix);

/// <summary>
/// Generación y hashing de claves de API. El hash es rápido (SHA-256) porque la clave es de
/// alta entropía (a diferencia de las contraseñas), apto para verificar en cada petición.
/// </summary>
public interface IApiKeyService
{
    /// <summary>Genera una nueva clave aleatoria con su hash y prefijo visible.</summary>
    GeneratedApiKey Generate();

    /// <summary>Calcula el hash de una clave presentada (para buscar/comparar).</summary>
    string Hash(string plainKey);
}
