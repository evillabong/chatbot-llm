namespace Mimo.Core.Interfaces;

/// <summary>
/// Publica eventos de dominio a los webhooks salientes del tenant activo. Crea una entrega
/// (encolada) por cada suscripción activa interesada en el evento; el envío HTTP real, con firma
/// HMAC y reintentos, lo hace un worker de background.
///
/// Es best-effort: nunca lanza hacia el llamante (un fallo de webhooks no debe tumbar la operación
/// que originó el evento). Opera sobre el esquema del tenant ya resuelto en la petición.
/// </summary>
public interface IWebhookPublisher
{
    /// <summary>
    /// Encola el <paramref name="payload"/> para todas las suscripciones activas al
    /// <paramref name="eventType"/>. El payload se serializa a JSON y se guarda tal cual se enviará.
    /// </summary>
    Task PublishAsync(string eventType, object payload, CancellationToken ct = default);
}
