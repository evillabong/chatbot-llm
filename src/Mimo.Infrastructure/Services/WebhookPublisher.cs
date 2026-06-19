using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IWebhookPublisher"/>: ante un evento, crea una entrega encolada por
/// cada suscripción activa interesada. El envío HTTP lo hace WebhookDeliveryWorker.
///
/// Opera sobre <see cref="TenantDbContext"/> (esquema del tenant ya resuelto). Best-effort: si algo
/// falla, lo registra y no propaga, para no afectar la operación que originó el evento.
/// </summary>
public sealed class WebhookPublisher(
    TenantDbContext db,
    ILogger<WebhookPublisher> logger) : IWebhookPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(string eventType, object payload, CancellationToken ct = default)
    {
        try
        {
            var subscriptions = await db.WebhookSubscriptions
                .Where(s => s.IsActive && s.Events.Contains(eventType))
                .Select(s => s.Id)
                .ToListAsync(ct);

            if (subscriptions.Count == 0)
                return;

            // Envelope estable: lo que se entrega y sobre lo que se firma (sin el id de entrega,
            // que viaja en cabecera por ser distinto en cada intento de cada suscripción).
            var body = JsonSerializer.Serialize(new
            {
                @event     = eventType,
                occurredAt = DateTime.UtcNow,
                data       = payload
            }, JsonOptions);

            foreach (var subscriptionId in subscriptions)
            {
                db.WebhookDeliveries.Add(new WebhookDelivery
                {
                    Id             = Guid.NewGuid(),
                    SubscriptionId = subscriptionId,
                    EventType      = eventType,
                    Payload        = body,
                    Status         = WebhookDeliveryStatus.Pending,
                    AttemptCount   = 0,
                    CreatedAt      = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo encolar el webhook del evento {EventType}.", eventType);
        }
    }
}
