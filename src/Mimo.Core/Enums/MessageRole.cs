namespace Mimo.Core.Enums;

/// <summary>
/// Rol del emisor de un mensaje dentro de una conversación.
/// </summary>
public enum MessageRole
{
    /// <summary>Mensaje enviado por el ciudadano.</summary>
    User,

    /// <summary>Respuesta generada por el bot de IA.</summary>
    Assistant,

    /// <summary>Mensaje interno del sistema (eventos, notificaciones).</summary>
    System,

    /// <summary>Mensaje enviado por un funcionario humano.</summary>
    Agent
}
