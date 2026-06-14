namespace Mimo.Core.Interfaces;

/// <summary>
/// Gateway de IA de la plataforma. Punto único por el que pasan TODAS las consultas al
/// LLM de los tenants. Aísla las credenciales del proveedor, mantiene la opacidad del
/// modelo frente al tenant, aplica los entitlements y cuotas del plan, y registra el uso.
///
/// Los consumidores de dominio (orquestador, búsqueda vectorial) usan este servicio en
/// lugar de <see cref="ILlmClient"/> directamente.
/// </summary>
public interface IAiGatewayService
{
    /// <summary>
    /// Genera una respuesta de chat para el tenant indicado, aplicando entitlements y
    /// cuotas del plan y registrando el uso.
    /// </summary>
    /// <exception cref="Mimo.Core.Exceptions.AiAccessDeniedException">El plan no permite el modelo.</exception>
    /// <exception cref="Mimo.Core.Exceptions.AiQuotaExceededException">Cuota del periodo alcanzada.</exception>
    Task<string> ChatAsync(
        Guid tenantId,
        string systemPrompt,
        IReadOnlyList<(string role, string content)> history,
        CancellationToken ct = default);

    /// <summary>
    /// Genera el embedding de un texto para el tenant indicado, aplicando entitlements y
    /// cuotas del plan y registrando el uso.
    /// </summary>
    /// <exception cref="Mimo.Core.Exceptions.AiAccessDeniedException">El plan no permite el modelo.</exception>
    /// <exception cref="Mimo.Core.Exceptions.AiQuotaExceededException">Cuota del periodo alcanzada.</exception>
    Task<float[]> GetEmbeddingAsync(Guid tenantId, string text, CancellationToken ct = default);
}
