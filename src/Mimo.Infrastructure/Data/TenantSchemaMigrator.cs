using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mimo.Infrastructure.Data;

/// <summary>
/// Aplica las migraciones de <see cref="TenantDbContext"/> a los esquemas de los tenants que YA
/// existen. El aprovisionamiento migra cada tenant al crearlo, pero las migraciones posteriores no
/// llegan solas a los tenants previos; este componente cierra esa brecha al arrancar la API.
/// Idempotente: <c>MigrateAsync</c> es no-op si el esquema está al día.
/// </summary>
public static partial class TenantSchemaMigrator
{
    public static async Task MigrateExistingTenantsAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var globalDb      = services.GetRequiredService<GlobalDbContext>();
        var tenantFactory = services.GetRequiredService<IDbContextFactory<TenantDbContext>>();
        var logger        = services.GetService<ILoggerFactory>()?.CreateLogger("TenantSchemaMigrator");

        var slugs = await globalDb.Tenants
            .Where(t => t.IsActive)
            .Select(t => t.Slug)
            .ToListAsync(ct);

        foreach (var slug in slugs)
        {
            var schema = BuildSchemaName(slug);
            try
            {
                await using var db = await tenantFactory.CreateDbContextAsync(ct);
                // Mantener UNA conexión abierta: con pooling el SET search_path se perdería si la
                // conexión vuelve al pool antes de migrar (mismo patrón que el aprovisionamiento).
                await db.Database.OpenConnectionAsync(ct);
                try
                {
                    // Identificador saneado por BuildSchemaName ([a-z0-9_]); no se parametriza en SET.
#pragma warning disable EF1002
                    await db.Database.ExecuteSqlRawAsync($"SET search_path TO \"{schema}\", public", ct);
#pragma warning restore EF1002
                    await db.Database.MigrateAsync(ct);
                }
                finally
                {
                    await db.Database.CloseConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "No se pudo migrar el esquema del tenant '{Slug}'.", slug);
            }
        }
    }

    private static string BuildSchemaName(string slug) =>
        "tenant_" + SafeSlugRegex().Replace(slug.ToLowerInvariant(), "").Replace('-', '_');

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SafeSlugRegex();
}
