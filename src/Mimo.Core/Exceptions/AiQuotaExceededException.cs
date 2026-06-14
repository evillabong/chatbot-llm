namespace Mimo.Core.Exceptions;

/// <summary>
/// Se lanza cuando el tenant alcanzó la cuota de uso de IA de su plan en el periodo.
/// Las APIs deben mapearla a 429 Too Many Requests.
/// </summary>
public class AiQuotaExceededException(string message) : Exception(message);
