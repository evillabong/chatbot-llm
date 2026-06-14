using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Construye el cliente LLM concreto a partir de un conector de IA.
/// La selección del conector (activo, permitido por plan) y el control de uso son
/// responsabilidad de <see cref="IAiGatewayService"/>; esta fábrica solo materializa
/// la implementación correspondiente al proveedor del conector.
/// </summary>
public interface ILlmClientFactory
{
    /// <summary>
    /// Crea un cliente LLM configurado con los parámetros del conector indicado.
    /// </summary>
    /// <exception cref="NotSupportedException">El proveedor del conector no está soportado.</exception>
    ILlmClient Create(AiConnector connector);
}
