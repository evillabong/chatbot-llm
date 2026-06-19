using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Integration;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Webhooks;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Gestión de webhooks SALIENTES del tenant (suscripciones a eventos). Bajo JWT/TenantAdmin, igual
/// que la gestión de API keys. Distinto de los webhooks ENTRANTES de canales (<see cref="WebhookEndpoints"/>,
/// <c>/webhooks/incoming</c>), donde la plataforma es el receptor; aquí es el emisor (ADR 0016).
///
/// La entrega real (HTTP + firma HMAC + reintentos) la hace WebhookDeliveryWorker.
/// </summary>
public static class WebhookSubscriptionEndpoints
{
    private const string SecretPrefix = "whsec_";

    public static IEndpointRouteBuilder MapWebhookSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/integration/webhooks")
            .WithTags("Integration")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListWebhooks")
            .WithSummary("Lista las suscripciones de webhook del tenant (sin el secreto).")
            .Produces<List<WebhookSubscriptionResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateWebhook")
            .WithSummary("Crea una suscripción de webhook; el secreto de firma se devuelve UNA sola vez.")
            .Produces<CreatedWebhookSubscriptionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/", RevokeAsync)
            .WithName("RevokeWebhook")
            .WithSummary("Revoca (desactiva) una suscripción de webhook (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/deliveries", ListDeliveriesAsync)
            .WithName("ListWebhookDeliveries")
            .WithSummary("Bitácora de entregas recientes de webhooks del tenant (query opcional: subscriptionId).")
            .Produces<List<WebhookDeliveryResponse>>();

        return app;
    }

    private static async Task<IResult> ListAsync(TenantDbContext db, CancellationToken ct = default)
    {
        var subs = await db.WebhookSubscriptions
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new WebhookSubscriptionResponse(
                s.Id, s.Name, s.Url, s.Events, s.IsActive, s.CreatedAt))
            .ToListAsync(ct);
        return Results.Ok(subs);
    }

    private static async Task<IResult> CreateAsync(
        CreateWebhookSubscriptionRequest request,
        TenantDbContext db,
        ISecretProtector secretProtector,
        CancellationToken ct = default)
    {
        // Validar que los eventos solicitados existan en el catálogo.
        var unknown = request.Events.Where(e => !WebhookEventTypes.All.Contains(e)).ToList();
        if (unknown.Count > 0)
            return Results.BadRequest(new { error = $"Eventos no soportados: {string.Join(", ", unknown)}." });

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return Results.BadRequest(new { error = "La URL debe ser http(s) absoluta." });

        // Secreto de firma: token aleatorio de alta entropía, cifrado en reposo (ADR 0010).
        var secret = SecretPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var entity = new WebhookSubscription
        {
            Id              = Guid.NewGuid(),
            Name            = request.Name,
            Url             = request.Url,
            SecretProtected = secretProtector.Protect(secret),
            Events          = request.Events.Distinct().ToList(),
            IsActive        = true,
            CreatedAt       = DateTime.UtcNow
        };

        db.WebhookSubscriptions.Add(entity);
        await db.SaveChangesAsync(ct);

        // El secreto en claro se devuelve solo aquí; no se puede recuperar después.
        return Results.Created("/integration/webhooks",
            new CreatedWebhookSubscriptionResponse(
                entity.Id, entity.Name, entity.Url, entity.Events, secret, entity.CreatedAt));
    }

    private static async Task<IResult> RevokeAsync(Guid id, TenantDbContext db, CancellationToken ct = default)
    {
        var sub = await db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sub is null)
            return Results.NotFound(new { error = "Suscripción no encontrada." });

        sub.IsActive  = false;
        sub.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListDeliveriesAsync(
        TenantDbContext db, Guid? subscriptionId = null, CancellationToken ct = default)
    {
        var query = db.WebhookDeliveries.AsNoTracking();
        if (subscriptionId is not null)
            query = query.Where(d => d.SubscriptionId == subscriptionId);

        var deliveries = await query
            .OrderByDescending(d => d.CreatedAt)
            .Take(100)
            .Select(d => new WebhookDeliveryResponse(
                d.Id, d.SubscriptionId, d.EventType, d.Status.ToString(), d.AttemptCount,
                d.ResponseStatusCode, d.LastError, d.LastAttemptAt, d.CreatedAt))
            .ToListAsync(ct);
        return Results.Ok(deliveries);
    }
}
