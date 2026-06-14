namespace Mimo.Core.Exceptions;

/// <summary>
/// Se lanza cuando el plan del tenant no permite el proveedor/modelo de IA solicitado.
/// Las APIs deben mapearla a 403 Forbidden.
/// </summary>
public class AiAccessDeniedException(string message) : Exception(message);
