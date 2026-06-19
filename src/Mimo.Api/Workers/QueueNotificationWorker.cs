using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Mimo.Api.Hubs;
using Mimo.Core.Enums;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Workers;

/// <summary>
/// Worker de background que notifica periódicamente a los agentes sobre
/// el estado de su cola de tickets pendientes.
/// Emite el evento "QueueStatus" al grupo "role:{roleId}" en TicketHub cada 30 segundos.
/// </summary>
public class QueueNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IHubContext<TicketHub> ticketHub,
    ILogger<QueueNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("QueueNotificationWorker iniciado.");
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BroadcastQueueStatusAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error al enviar notificación de cola.");
            }
        }
    }

    private async Task BroadcastQueueStatusAsync(CancellationToken ct)
    {
        await using var scope    = scopeFactory.CreateAsyncScope();
        var globalDb             = scope.ServiceProvider.GetRequiredService<GlobalDbContext>();
        var tenantDbFactory      = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();

        var tenants = await globalDb.Tenants
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Slug })
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                var schema = BuildSchemaName(tenant.Slug);
                await using var db = await tenantDbFactory.CreateDbContextAsync(ct);

                // El contexto del factory no lleva el SearchPathConnectionInterceptor; hay que fijar el
                // search_path manualmente y, con pooling, SET y consulta deben ir por la MISMA conexión
                // (de lo contrario la consulta tomaría otra conexión apuntando a public → 42P01). Por
                // eso se abre una conexión explícita. El identificador está saneado por BuildSchemaName.
                List<(Guid? RoleId, int Count)> queueStats;
                await db.Database.OpenConnectionAsync(ct);
                try
                {
#pragma warning disable EF1002
                    await db.Database.ExecuteSqlRawAsync($"SET search_path TO \"{schema}\", public", ct);
#pragma warning restore EF1002

                    // Obtener número de tickets en cola por rol
                    queueStats = (await db.Tickets
                        .Where(t => t.Status == TicketStatus.InQueue)
                        .GroupBy(t => t.AssignedRoleId)
                        .Select(g => new { RoleId = g.Key, Count = g.Count() })
                        .ToListAsync(ct))
                        .Select(x => ((Guid?)x.RoleId, x.Count))
                        .ToList();
                }
                finally
                {
                    await db.Database.CloseConnectionAsync();
                }

                // Notificar a los agentes de cada rol
                foreach (var stat in queueStats)
                {
                    await ticketHub.Clients
                        .Group($"role:{stat.RoleId}")
                        .SendAsync("QueueStatus", new
                        {
                            RoleId    = stat.RoleId,
                            InQueue   = stat.Count,
                            Timestamp = DateTime.UtcNow
                        }, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error notificando cola para tenant {TenantId}.", tenant.Id);
            }
        }
    }

    private static string BuildSchemaName(string slug) =>
        "tenant_" + System.Text.RegularExpressions.Regex
            .Replace(slug.ToLowerInvariant(), @"[^a-z0-9\-]", "")
            .Replace('-', '_');
}
