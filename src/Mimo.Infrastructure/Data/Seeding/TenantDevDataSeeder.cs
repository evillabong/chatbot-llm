using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mimo.Core.Authorization;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using System.Text.RegularExpressions;

namespace Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// Siembra datos de DESARROLLO dentro del esquema de cada tenant activo: roles adicionales,
/// funcionarios de demo con credenciales conocidas y conocimiento de ejemplo. Idempotente
/// (por nombre/email/título); no toca el rol/admin creados durante el aprovisionamiento.
/// </summary>
public partial class TenantDevDataSeeder(
    IDbContextFactory<TenantDbContext> contextFactory,
    GlobalDbContext globalDb,
    IPasswordHasher passwordHasher,
    ILogger<TenantDevDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenants = await globalDb.Tenants
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Slug })
            .ToListAsync(ct);

        foreach (var tenant in tenants)
            await SeedTenantAsync(tenant.Id, tenant.Slug, ct);
    }

    private async Task SeedTenantAsync(Guid tenantId, string slug, CancellationToken ct)
    {
        var schema = BuildSchemaName(slug);
        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // Una sola conexión abierta para que el SET search_path no se pierda con el pooling.
        await db.Database.OpenConnectionAsync(ct);
        try
        {
#pragma warning disable EF1002 // identificador saneado; no parametrizable en SET
            await db.Database.ExecuteSqlRawAsync($"SET search_path TO \"{schema}\", public", ct);
#pragma warning restore EF1002

            await SeedRolesAndAgentsAsync(db, tenantId, slug, ct);
            await SeedKnowledgeAsync(db, tenantId, ct);

            logger.LogInformation("Datos de desarrollo sembrados en el esquema '{Schema}'", schema);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task SeedRolesAndAgentsAsync(TenantDbContext db, Guid tenantId, string slug, CancellationToken ct)
    {
        // Roles adicionales (el rol "Administrador" lo crea el aprovisionamiento).
        var roleSpecs = new (string Name, string Description, int Priority, bool ViewAll)[]
        {
            ("Supervisor", "Supervisa la operación y ve todos los tickets.", 80, true),
            ("Soporte",    "Atiende solicitudes de soporte al cliente.",      50, false),
            ("Ventas",     "Gestiona oportunidades y consultas comerciales.", 50, false)
        };

        var rolesByName = await db.Roles.ToDictionaryAsync(r => r.Name, ct);
        foreach (var (name, description, priority, viewAll) in roleSpecs)
        {
            if (rolesByName.ContainsKey(name)) continue;

            var role = new Role
            {
                Id                = Guid.NewGuid(),
                TenantId          = tenantId,
                Name              = name,
                Description       = description,
                PriorityLevel     = priority,
                CanViewAllTickets = viewAll,
                IsActive          = true,
                CreatedAt         = DateTime.UtcNow
            };
            db.Roles.Add(role);
            rolesByName[name] = role;
        }
        await db.SaveChangesAsync(ct);

        // Funcionarios de demo con credenciales conocidas (idempotente por email).
        // "tenantadmin" lleva el rol Administrador para poder usar el CRUD de admin de tenant.
        var agentSpecs = new (string Email, string FullName, string Alias, string RoleName)[]
        {
            ($"tenantadmin@{slug}.local", "Administrador Demo", "Admin",   MimoAuthorization.Roles.Administrator),
            ($"supervisor@{slug}.local",  "Supervisor Demo",    "Supe",    "Supervisor"),
            ($"soporte@{slug}.local",     "Agente de Soporte",  "Soporte", "Soporte"),
            ($"ventas@{slug}.local",      "Agente de Ventas",   "Ventas",  "Ventas")
        };

        var hash = passwordHasher.Hash(DevSeedDefaults.Password);
        foreach (var (email, fullName, alias, roleName) in agentSpecs)
        {
            if (await db.Agents.AnyAsync(a => a.Email == email, ct)) continue;

            var agent = new Agent
            {
                Id                    = Guid.NewGuid(),
                TenantId              = tenantId,
                Email                 = email,
                PasswordHash          = hash,
                FullName              = fullName,
                Alias                 = alias,
                MaxConcurrentSessions = 3,
                IsActive              = true,
                CreatedAt             = DateTime.UtcNow
            };

            if (rolesByName.TryGetValue(roleName, out var role))
                agent.AgentRoles.Add(new AgentRole { AgentId = agent.Id, RoleId = role.Id });

            db.Agents.Add(agent);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedKnowledgeAsync(TenantDbContext db, Guid tenantId, CancellationToken ct)
    {
        var categoriesByName = await db.DocumentCategories.ToDictionaryAsync(c => c.Name, ct);
        foreach (var name in new[] { "General", "Soporte", "Ventas" })
        {
            if (categoriesByName.ContainsKey(name)) continue;

            var category = new DocumentCategory { Id = Guid.NewGuid(), TenantId = tenantId, Name = name };
            db.DocumentCategories.Add(category);
            categoriesByName[name] = category;
        }
        await db.SaveChangesAsync(ct);

        // Documentos de ejemplo (sin embedding; se pueden reindexar luego desde la UI).
        var docSpecs = new (string Title, string Content, string Category, VisibilityLevel Visibility)[]
        {
            ("Horario de atención", "Atendemos de lunes a viernes de 8:00 a 18:00.", "General", VisibilityLevel.Public),
            ("Cómo restablecer mi contraseña", "Usa la opción '¿Olvidaste tu contraseña?' en el inicio de sesión.", "Soporte", VisibilityLevel.Public),
            ("Catálogo de productos", "Consulta nuestro catálogo vigente con un agente de ventas.", "Ventas", VisibilityLevel.Private)
        };

        foreach (var (title, content, categoryName, visibility) in docSpecs)
        {
            if (await db.Documents.AnyAsync(d => d.Title == title, ct)) continue;

            categoriesByName.TryGetValue(categoryName, out var category);
            db.Documents.Add(new Document
            {
                Id            = Guid.NewGuid(),
                TenantId      = tenantId,
                Title         = title,
                Content       = content,
                Visibility    = visibility,
                CategoryId    = category?.Id,
                PriorityLevel = 0,
                IsActive      = true,
                CreatedAt     = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static string BuildSchemaName(string slug)
    {
        var sanitized = SafeSlugRegex().Replace(slug.ToLowerInvariant(), "");
        return $"tenant_{sanitized.Replace('-', '_')}";
    }

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SafeSlugRegex();
}
