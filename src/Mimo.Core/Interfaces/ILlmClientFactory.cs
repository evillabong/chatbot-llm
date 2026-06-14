namespace Mimo.Core.Interfaces;

/// <summary>
/// Fábrica que resuelve el cliente LLM correspondiente al conector de IA activo
/// configurado en la base de datos. Aísla a los consumidores (orquestador, búsqueda
/// vectorial) del proveedor concreto y permite cambiar de IA sin tocar su código.
/// </summary>
public interface ILlmClientFactory
{
    /// <summary>
    /// Obtiene un cliente LLM configurado con los parámetros del conector activo.
    /// Lanza si no hay ningún conector de IA activo.
    /// </summary>
    Task<ILlmClient> GetActiveClientAsync(CancellationToken ct = default);
}
