namespace Mimo.Core.Models;

/// <summary>Estado de una entrega de webhook.</summary>
public enum WebhookDeliveryStatus
{
    /// <summary>Encolada o a la espera del próximo reintento.</summary>
    Pending = 0,

    /// <summary>Entregada con éxito (respuesta 2xx).</summary>
    Delivered = 1,

    /// <summary>Agotados los reintentos sin éxito (terminal).</summary>
    Failed = 2
}

/// <summary>
/// Intento de entrega de un evento a una suscripción. Sirve de cola (el worker procesa las
/// pendientes) y de bitácora (qué se envió, cuándo y con qué resultado).
/// Vive en el esquema del tenant.
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; set; }

    /// <summary>Suscripción destino.</summary>
    public Guid SubscriptionId { get; set; }
    public WebhookSubscription? Subscription { get; set; }

    /// <summary>Tipo de evento (p. ej. "ticket.resolved").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Cuerpo JSON exacto que se envía y sobre el que se calcula la firma HMAC.</summary>
    public string Payload { get; set; } = string.Empty;

    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;

    /// <summary>Número de intentos realizados.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Momento a partir del cual puede reintentarse (backoff). Null = listo ya.</summary>
    public DateTime? NextAttemptAt { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    /// <summary>Código HTTP de la última respuesta, si hubo.</summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>Último error (mensaje corto) para diagnóstico.</summary>
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
