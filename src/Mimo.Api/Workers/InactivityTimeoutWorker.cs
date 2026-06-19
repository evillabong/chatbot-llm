using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mimo.Core.Enums;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Workers;

/// <summary>
/// Worker de background que cierra conversaciones inactivas.
/// Escanea todas las conversaciones BotActive cuyo último mensaje supere
/// el umbral de inactividad configurado por el tenant.
/// Ejecuta cada 5 minutos.
/// </summary>
public class InactivityTimeoutWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<InactivityTimeoutWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval         = TimeSpan.FromMinutes(5);
    private const           int      DefaultTimeoutMin = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InactivityTimeoutWorker iniciado.");
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error al procesar inactividad de conversaciones.");
            }
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await using var scope   = scopeFactory.CreateAsyncScope();
        var globalDb            = scope.ServiceProvider.GetRequiredService<GlobalDbContext>();
        var tenantDbFactory     = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();

        // Obtener todos los tenants activos
        var tenants = await globalDb.Tenants
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Slug, t.Configuration })
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            var timeoutMin = tenant.Configuration?.CustomerAttention?.InactivityTimeoutMinutes
                             ?? DefaultTimeoutMin;

            if (timeoutMin <= 0) continue;

            var cutoff = DateTime.UtcNow.AddMinutes(-timeoutMin);

            try
            {
                var schema = BuildSchemaName(tenant.Slug);
                await using var db = await tenantDbFactory.CreateDbContextAsync(ct);

                // El contexto del factory no lleva el SearchPathConnectionInterceptor; hay que fijar el
                // search_path manualmente y, con pooling, SET y consulta deben ir por la MISMA conexión
                // (de lo contrario tomaría otra del pool apuntando a public). Por eso se abre una
                // conexión explícita. ExecuteSqlAsync parametrizaba el identificador ("@p0") → 42601;
                // el nombre está saneado por BuildSchemaName, se interpola en SQL crudo.
                int closed;
                await db.Database.OpenConnectionAsync(ct);
                try
                {
#pragma warning disable EF1002
                    await db.Database.ExecuteSqlRawAsync($"SET search_path TO \"{schema}\", public", ct);
#pragma warning restore EF1002

                    // Cerrar conversaciones BotActive sin actividad reciente
                    closed = await db.Conversations
                        .Where(c => c.Status == TicketStatus.BotActive
                                 && c.LastMessageAt < cutoff)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(c => c.Status,    TicketStatus.Closed)
                                   .SetProperty(c => c.ResolvedAt, DateTime.UtcNow),
                            ct);
                }
                finally
                {
                    await db.Database.CloseConnectionAsync();
                }

                if (closed > 0)
                    logger.LogInformation(
                        "Tenant {TenantId}: {Count} conversaciones cerradas por inactividad.",
                        tenant.Id, closed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando inactividad para tenant {TenantId}.", tenant.Id);
            }
        }
    }

    private static string BuildSchemaName(string slug) =>
        "tenant_" + System.Text.RegularExpressions.Regex
            .Replace(slug.ToLowerInvariant(), @"[^a-z0-9\-]", "")
            .Replace('-', '_');
}
