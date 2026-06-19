using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Workers;

/// <summary>
/// Entrega los webhooks salientes pendientes (ADR 0016). Recorre los tenants activos, fija el
/// search_path de cada uno y procesa las entregas listas: POST del payload con firma HMAC-SHA256,
/// registro del resultado y reintentos con backoff exponencial hasta agotarse.
///
/// Sigue el patrón multi-tenant de <see cref="QueueNotificationWorker"/>: el worker corre fuera de
/// una petición, así que fija el esquema manualmente por tenant.
/// </summary>
public sealed class WebhookDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    public const string HttpClientName = "Webhooks";

    private static readonly TimeSpan Interval        = TimeSpan.FromSeconds(15);
    private const int                MaxAttempts     = 5;
    private const int                BatchPerTenant  = 50;
    private const string             SignatureHeader = "X-Mimo-Signature";
    private const string             EventHeader     = "X-Mimo-Event";
    private const string             DeliveryHeader  = "X-Mimo-Delivery";
    private const string             TimestampHeader = "X-Mimo-Timestamp";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WebhookDeliveryWorker iniciado.");
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error en el ciclo de entrega de webhooks.");
            }
        }
    }

    private async Task ProcessAllTenantsAsync(CancellationToken ct)
    {
        await using var scope   = scopeFactory.CreateAsyncScope();
        var globalDb            = scope.ServiceProvider.GetRequiredService<GlobalDbContext>();
        var tenantDbFactory     = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();
        var secretProtector     = scope.ServiceProvider.GetRequiredService<ISecretProtector>();

        var slugs = await globalDb.Tenants
            .Where(t => t.IsActive)
            .Select(t => t.Slug)
            .ToListAsync(ct);

        foreach (var slug in slugs)
        {
            try
            {
                await ProcessTenantAsync(slug, tenantDbFactory, secretProtector, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error entregando webhooks del tenant {Slug}.", slug);
            }
        }
    }

    private async Task ProcessTenantAsync(
        string slug,
        IDbContextFactory<TenantDbContext> tenantDbFactory,
        ISecretProtector secretProtector,
        CancellationToken ct)
    {
        var schema = BuildSchemaName(slug);
        await using var db = await tenantDbFactory.CreateDbContextAsync(ct);

        // El contexto del FACTORY no lleva el SearchPathConnectionInterceptor (solo el scoped de DI),
        // así que hay que fijar el search_path manualmente. Con pooling, SET y consulta deben ir por
        // la MISMA conexión: por eso se abre una conexión explícita durante cada bloque de BD (si se
        // dejara cerrar, la siguiente operación tomaría otra conexión del pool, apuntando a public).
        List<WebhookDelivery> pending;
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await SetSearchPathAsync(db, schema, ct);

            var now = DateTime.UtcNow;
            // Entregas listas: pendientes sin programación o con el backoff ya cumplido.
            pending = await db.WebhookDeliveries
                .Where(d => d.Status == WebhookDeliveryStatus.Pending &&
                            (d.NextAttemptAt == null || d.NextAttemptAt <= now))
                .OrderBy(d => d.CreatedAt)
                .Take(BatchPerTenant)
                .Include(d => d.Subscription)
                .ToListAsync(ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        if (pending.Count == 0)
            return;

        // El POST HTTP se hace SIN conexión de BD tomada (no bloquear el pool durante la red).
        var client = httpClientFactory.CreateClient(HttpClientName);
        foreach (var delivery in pending)
        {
            await AttemptDeliveryAsync(client, delivery, secretProtector, ct);
        }

        // Persistir los cambios (estado/intentos) reabriendo y fijando el search_path de nuevo.
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await SetSearchPathAsync(db, schema, ct);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static Task SetSearchPathAsync(TenantDbContext db, string schema, CancellationToken ct)
    {
        // Identificador saneado por BuildSchemaName ([a-z0-9_]); no se puede parametrizar un
        // identificador en SET (ExecuteSqlAsync produciría "SET ... TO @p0" → 42601), se interpola.
#pragma warning disable EF1002
        return db.Database.ExecuteSqlRawAsync($"SET search_path TO \"{schema}\", public", ct);
#pragma warning restore EF1002
    }

    private async Task AttemptDeliveryAsync(
        HttpClient client, WebhookDelivery delivery, ISecretProtector secretProtector, CancellationToken ct)
    {
        delivery.AttemptCount++;
        delivery.LastAttemptAt = DateTime.UtcNow;

        var sub = delivery.Subscription;
        if (sub is null || !sub.IsActive)
        {
            delivery.Status    = WebhookDeliveryStatus.Failed;
            delivery.LastError = "La suscripción ya no está activa.";
            return;
        }

        try
        {
            var secret    = secretProtector.Unprotect(sub.SecretProtected);
            var signature = ComputeSignature(secret, delivery.Payload);

            using var content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json");
            content.Headers.Add(SignatureHeader, $"sha256={signature}");
            content.Headers.Add(EventHeader, delivery.EventType);
            content.Headers.Add(DeliveryHeader, delivery.Id.ToString());
            content.Headers.Add(TimestampHeader, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            using var response = await client.PostAsync(sub.Url, content, ct);
            delivery.ResponseStatusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                delivery.Status        = WebhookDeliveryStatus.Delivered;
                delivery.LastError     = null;
                delivery.NextAttemptAt = null;
            }
            else
            {
                ScheduleRetryOrFail(delivery, $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            delivery.ResponseStatusCode = null;
            ScheduleRetryOrFail(delivery, ex.Message);
        }
    }

    private void ScheduleRetryOrFail(WebhookDelivery delivery, string error)
    {
        delivery.LastError = Truncate(error, 1000);

        if (delivery.AttemptCount >= MaxAttempts)
        {
            delivery.Status        = WebhookDeliveryStatus.Failed;
            delivery.NextAttemptAt = null;
            logger.LogWarning("Webhook {DeliveryId} agotó reintentos: {Error}", delivery.Id, error);
        }
        else
        {
            delivery.Status        = WebhookDeliveryStatus.Pending;
            delivery.NextAttemptAt = DateTime.UtcNow + Backoff(delivery.AttemptCount);
        }
    }

    /// <summary>HMAC-SHA256 hex (minúsculas) del cuerpo, con el secreto de la suscripción.</summary>
    private static string ComputeSignature(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }

    private static TimeSpan Backoff(int attempt) => attempt switch
    {
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        3 => TimeSpan.FromMinutes(15),
        _ => TimeSpan.FromHours(1)
    };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string BuildSchemaName(string slug) =>
        "tenant_" + System.Text.RegularExpressions.Regex
            .Replace(slug.ToLowerInvariant(), @"[^a-z0-9\-]", "")
            .Replace('-', '_');
}
