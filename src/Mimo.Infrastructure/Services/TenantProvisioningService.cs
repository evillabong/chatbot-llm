using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mimo.Core.Authorization;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace Mimo.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de aprovisionamiento de tenants.
/// Al crear un tenant, genera el esquema PostgreSQL y aplica las migraciones de TenantDbContext,
/// garantizando que la estructura de tablas esté siempre sincronizada con el código.
/// </summary>
public partial class TenantProvisioningService(
    IDbContextFactory<TenantDbContext> contextFactory,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    public async Task ProvisionAsync(Tenant tenant, TenantAdminSeed admin, CancellationToken ct = default)
    {
        var schema = BuildSchemaName(tenant.Slug);
        logger.LogInformation("Aprovisionando esquema '{Schema}' para tenant '{Slug}'", schema, tenant.Slug);

        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // 1. Crear el esquema en PostgreSQL si no existe
        await db.Database.ExecuteSqlAsync($"CREATE SCHEMA IF NOT EXISTS {schema}", ct);

        // 2. Apuntar el contexto al nuevo esquema y ejecutar las migraciones pendientes
        await db.Database.ExecuteSqlAsync($"SET search_path TO {schema}, public", ct);
        await db.Database.MigrateAsync(ct);

        // 3. Crear el rol "Administrador" (acceso total) y el funcionario administrador inicial,
        //    para que el tenant pueda iniciar sesión inmediatamente vía POST /auth/login.
        var adminRole = new Role
        {
            Id                = Guid.NewGuid(),
            TenantId          = tenant.Id,
            Name              = MimoAuthorization.Roles.Administrator,
            Description       = "Rol con acceso total a la configuración y a todos los tickets del tenant.",
            PriorityLevel     = 100,
            CanViewAllTickets = true,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow
        };

        var adminAgent = new Agent
        {
            Id           = Guid.NewGuid(),
            TenantId     = tenant.Id,
            Email        = admin.Email,
            PasswordHash = admin.PasswordHash,
            FullName     = admin.FullName,
            Alias        = admin.Alias,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        adminAgent.AgentRoles.Add(new AgentRole { AgentId = adminAgent.Id, RoleId = adminRole.Id });

        db.Roles.Add(adminRole);
        db.Agents.Add(adminAgent);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Esquema '{Schema}' aprovisionado correctamente con administrador '{Email}'", schema, admin.Email);
    }

    public async Task DeprovisionAsync(string slug, CancellationToken ct = default)
    {
        var schema = BuildSchemaName(slug);
        logger.LogWarning("Eliminando esquema '{Schema}' del tenant '{Slug}'", schema, slug);

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlAsync($"DROP SCHEMA IF EXISTS {schema} CASCADE", ct);

        logger.LogWarning("Esquema '{Schema}' eliminado", schema);
    }

    /// <summary>
    /// Construye el nombre del esquema PostgreSQL a partir del slug del tenant.
    /// Solo permite letras minúsculas, dígitos y guiones bajos.
    /// </summary>
    private static string BuildSchemaName(string slug)
    {
        var sanitized = SafeSlugRegex().Replace(slug.ToLowerInvariant(), "");
        return $"tenant_{sanitized.Replace('-', '_')}";
    }

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SafeSlugRegex();
}
