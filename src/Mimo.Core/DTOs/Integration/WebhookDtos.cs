using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Integration;

/// <summary>Metadatos de una suscripción de webhook (nunca incluye el secreto).</summary>
public record WebhookSubscriptionResponse(
    Guid Id,
    string Name,
    string Url,
    IReadOnlyList<string> Events,
    bool IsActive,
    DateTime CreatedAt
);

/// <summary>
/// Respuesta al crear una suscripción: incluye el <see cref="Secret"/> en claro, que se muestra
/// UNA sola vez (el cliente lo necesita para verificar la firma HMAC). No se puede recuperar después.
/// </summary>
public record CreatedWebhookSubscriptionResponse(
    Guid Id,
    string Name,
    string Url,
    IReadOnlyList<string> Events,
    string Secret,
    DateTime CreatedAt
);

/// <summary>Solicitud para crear una suscripción de webhook.</summary>
public record CreateWebhookSubscriptionRequest(
    [Required, MaxLength(120)] string Name,
    [Required, Url, MaxLength(2048)] string Url,
    [Required, MinLength(1)] List<string> Events
);

/// <summary>Entrada de bitácora de una entrega de webhook.</summary>
public record WebhookDeliveryResponse(
    Guid Id,
    Guid SubscriptionId,
    string EventType,
    string Status,
    int AttemptCount,
    int? ResponseStatusCode,
    string? LastError,
    DateTime? LastAttemptAt,
    DateTime CreatedAt
);
