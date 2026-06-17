using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mimo.Core.Authorization;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;
using System.Text.RegularExpressions;

namespace Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// Siembra un negocio de demostración coherente (AndinaShop, tienda online) con los cuatro
/// actores: SuperAdmin (plataforma, sembrado aparte), Admin del tenant, funcionarios (ventas,
/// soporte, envíos, supervisor) y clientes (conversaciones con mensajes, tickets y encuestas).
/// Solo en Development. Idempotente: si el tenant ya existe no re-aprovisiona, y cada bloque
/// comprueba existencia antes de insertar.
/// </summary>
public sealed partial class BusinessDemoSeeder(
    GlobalDbContext globalDb,
    ITenantProvisioningService provisioning,
    IDbContextFactory<TenantDbContext> contextFactory,
    IPasswordHasher passwordHasher,
    ILogger<BusinessDemoSeeder> logger)
{
    private const string Slug = DevSeedDefaults.BusinessTenantSlug;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await EnsureTenantAsync(ct);
        await SeedTenantDataAsync(tenant.Id, ct);
    }

    /// <summary>Crea y aprovisiona el tenant del negocio si aún no existe.</summary>
    private async Task<Tenant> EnsureTenantAsync(CancellationToken ct)
    {
        var existing = await globalDb.Tenants.FirstOrDefaultAsync(t => t.Slug == Slug, ct);
        if (existing is not null)
            return existing;

        var tenant = new Tenant
        {
            Id            = Guid.NewGuid(),
            Name          = DevSeedDefaults.BusinessTenantName,
            Slug          = Slug,
            Plan          = "pro",
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow,
            Configuration = BuildConfiguration()
        };

        var admin = new TenantAdminSeed(
            Email:        DevSeedDefaults.BusinessAdminEmail,
            PasswordHash: passwordHasher.Hash(DevSeedDefaults.Password),
            FullName:     "Administrador AndinaShop",
            Alias:        "Admin");

        // Aprovisiona el esquema + rol Administrador + funcionario admin (atómico).
        await provisioning.ProvisionAsync(tenant, admin, ct);

        globalDb.Tenants.Add(tenant);
        await globalDb.SaveChangesAsync(ct);

        logger.LogInformation("Tenant de demo '{Slug}' creado y aprovisionado", Slug);
        return tenant;
    }

    private async Task SeedTenantDataAsync(Guid tenantId, CancellationToken ct)
    {
        var schema = $"tenant_{SafeSlugRegex().Replace(Slug.Replace("-", "_"), "")}";
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        try
        {
#pragma warning disable EF1002 // identificador saneado; no parametrizable en SET
            await db.Database.ExecuteSqlRawAsync($"SET search_path TO \"{schema}\", public", ct);
#pragma warning restore EF1002

            var roles  = await SeedRolesAsync(db, tenantId, ct);
            var agents = await SeedAgentsAsync(db, tenantId, roles, ct);
            await SeedKnowledgeAsync(db, tenantId, ct);
            await SeedCustomersAsync(db, tenantId, roles, agents, ct);

            logger.LogInformation("Datos de negocio sembrados en '{Schema}'", schema);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(TenantDbContext db, Guid tenantId, CancellationToken ct)
    {
        var specs = new (string Name, string Description, int Priority, bool ViewAll)[]
        {
            ("Ventas",     "Atiende oportunidades de compra y consultas comerciales.", 60, false),
            ("Soporte",    "Resuelve incidencias post-venta y dudas de producto.",     60, false),
            ("Envíos",     "Gestiona estados de pedido, despachos y logística.",       60, false),
            ("Supervisor", "Supervisa la operación y ve todos los tickets.",           90, true)
        };

        var byName = await db.Roles.ToDictionaryAsync(r => r.Name, ct);
        foreach (var (name, description, priority, viewAll) in specs)
        {
            if (byName.ContainsKey(name)) continue;
            var role = new Role
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Name = name, Description = description,
                PriorityLevel = priority, CanViewAllTickets = viewAll, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            db.Roles.Add(role);
            byName[name] = role;
        }
        await db.SaveChangesAsync(ct);
        return byName;
    }

    private async Task<Dictionary<string, Agent>> SeedAgentsAsync(
        TenantDbContext db, Guid tenantId, Dictionary<string, Role> roles, CancellationToken ct)
    {
        var hash = passwordHasher.Hash(DevSeedDefaults.Password);
        var specs = new (string Email, string FullName, string Alias, string Role)[]
        {
            ("laura.gomez@andinashop.com",   "Laura Gómez",    "Laura",  "Ventas"),
            ("carlos.ruiz@andinashop.com",   "Carlos Ruiz",    "Carlos", "Soporte"),
            ("sofia.martinez@andinashop.com","Sofía Martínez", "Sofía",  "Envíos"),
            ("diego.herrera@andinashop.com", "Diego Herrera",  "Diego",  "Supervisor")
        };

        var byEmail = await db.Agents.ToDictionaryAsync(a => a.Email, ct);
        foreach (var (email, fullName, alias, roleName) in specs)
        {
            if (byEmail.ContainsKey(email)) continue;
            var agent = new Agent
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Email = email, PasswordHash = hash,
                FullName = fullName, Alias = alias, MaxConcurrentSessions = 4, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            if (roles.TryGetValue(roleName, out var role))
                agent.AgentRoles.Add(new AgentRole { AgentId = agent.Id, RoleId = role.Id });
            db.Agents.Add(agent);
            byEmail[email] = agent;
        }
        await db.SaveChangesAsync(ct);
        return byEmail;
    }

    private async Task SeedKnowledgeAsync(TenantDbContext db, Guid tenantId, CancellationToken ct)
    {
        var docs = new (string Title, string Content, VisibilityLevel Visibility)[]
        {
            ("Política de envíos", "Despachamos en 24-48 h hábiles. Envío gratis en compras superiores a $200.000. Cobertura nacional 3-6 días hábiles.", VisibilityLevel.Public),
            ("Política de devoluciones", "Aceptamos devoluciones dentro de los 30 días con factura y empaque original. El reembolso se procesa en 5-10 días hábiles.", VisibilityLevel.Public),
            ("Métodos de pago", "Aceptamos tarjetas de crédito/débito, PSE y pago contra entrega en ciudades principales.", VisibilityLevel.Public),
            ("Garantía de productos", "Todos los electrodomésticos incluyen garantía del fabricante de 12 meses. Los accesorios, 3 meses.", VisibilityLevel.Public),
            ("Guía de tallas y compatibilidad", "Consulta la compatibilidad de repuestos y accesorios con un agente de soporte antes de comprar.", VisibilityLevel.Private)
        };

        foreach (var (title, content, visibility) in docs)
        {
            if (await db.Documents.AnyAsync(d => d.Title == title, ct)) continue;
            db.Documents.Add(new Document
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Title = title, Content = content,
                Visibility = visibility, PriorityLevel = 0, IsActive = true, CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Siembra clientes (conversaciones) con mensajes, tickets y encuestas.</summary>
    private async Task SeedCustomersAsync(
        TenantDbContext db, Guid tenantId, Dictionary<string, Role> roles, Dictionary<string, Agent> agents, CancellationToken ct)
    {
        if (await db.Conversations.AnyAsync(ct))
            return; // ya sembrado

        var now = DateTime.UtcNow;

        // 1) Cliente atendido por el bot (sin escalar).
        var c1 = NewConversation(tenantId, Channel.WhatsApp, "wa:573001112233", "Pedro Ramírez",
            phone: "+57 300 111 2233", status: TicketStatus.BotActive, created: now.AddHours(-2));
        AddMessage(c1, MessageRole.User, "Hola, ¿hacen envíos a Medellín?", now.AddHours(-2));
        AddMessage(c1, MessageRole.Assistant, "¡Hola Pedro! Sí, enviamos a todo el país. A Medellín llega en 3-4 días hábiles y es gratis desde $200.000.", now.AddHours(-2).AddMinutes(1));
        db.Conversations.Add(c1);

        // 2) Venta: lead caliente escalado a Ventas (Laura), asignado.
        var c2 = NewConversation(tenantId, Channel.WebChat, "web:ana-torres", "Ana Torres",
            email: "ana.torres@example.com", authenticated: true, status: TicketStatus.Assigned, created: now.AddHours(-5));
        AddMessage(c2, MessageRole.User, "Quiero comprar una nevera de 400L, ¿qué modelos tienen y cuál recomiendan?", now.AddHours(-5));
        AddMessage(c2, MessageRole.Assistant, "Tenemos varios modelos no-frost de 400L. Te conecto con un asesor de ventas para una recomendación personalizada.", now.AddHours(-5).AddMinutes(1));
        AddMessage(c2, MessageRole.Agent, "Hola Ana, soy Laura de ventas. Te comparto 3 opciones según tu presupuesto.", now.AddHours(-4), senderId: agents["laura.gomez@andinashop.com"].Id);
        AddTicket(c2, roles["Ventas"].Id, agents["laura.gomez@andinashop.com"].Id,
            TicketStatus.Assigned, TicketPriority.High, assignedAt: now.AddHours(-4).AddMinutes(-2), firstResponseAt: now.AddHours(-4));
        db.Conversations.Add(c2);

        // 3) Envíos: estado de pedido, en progreso (Sofía).
        var c3 = NewConversation(tenantId, Channel.WhatsApp, "wa:573009998877", "María González",
            phone: "+57 300 999 8877", status: TicketStatus.InProgress, created: now.AddDays(-1));
        AddMessage(c3, MessageRole.User, "¿Cuál es el estado de mi pedido #10532?", now.AddDays(-1));
        AddMessage(c3, MessageRole.Assistant, "Déjame verificar tu pedido y te conecto con el área de envíos.", now.AddDays(-1).AddMinutes(1));
        AddMessage(c3, MessageRole.Agent, "Hola María, tu pedido #10532 salió de bodega y está en reparto, llega mañana antes de las 5 p.m.", now.AddDays(-1).AddMinutes(20), senderId: agents["sofia.martinez@andinashop.com"].Id);
        AddTicket(c3, roles["Envíos"].Id, agents["sofia.martinez@andinashop.com"].Id,
            TicketStatus.InProgress, TicketPriority.Normal, assignedAt: now.AddDays(-1).AddMinutes(5), firstResponseAt: now.AddDays(-1).AddMinutes(20));
        db.Conversations.Add(c3);

        // 4) Soporte: devolución, resuelto y cerrado con encuesta (Carlos).
        var c4 = NewConversation(tenantId, Channel.WebChat, "web:juan-perez", "Juan Pérez",
            email: "juan.perez@example.com", authenticated: true, status: TicketStatus.Closed,
            created: now.AddDays(-3), resolved: now.AddDays(-2));
        AddMessage(c4, MessageRole.User, "Recibí una licuadora con un defecto, quiero hacer la devolución.", now.AddDays(-3));
        AddMessage(c4, MessageRole.Assistant, "Lamento el inconveniente. Te conecto con soporte para gestionar la devolución.", now.AddDays(-3).AddMinutes(1));
        AddMessage(c4, MessageRole.Agent, "Hola Juan, soy Carlos. Genero la guía de devolución sin costo y el reembolso al confirmar la recepción.", now.AddDays(-3).AddMinutes(15), senderId: agents["carlos.ruiz@andinashop.com"].Id);
        AddMessage(c4, MessageRole.User, "¡Perfecto, muchas gracias!", now.AddDays(-2).AddHours(-1));
        var ticket4 = AddTicket(c4, roles["Soporte"].Id, agents["carlos.ruiz@andinashop.com"].Id,
            TicketStatus.Closed, TicketPriority.Normal, assignedAt: now.AddDays(-3).AddMinutes(5),
            firstResponseAt: now.AddDays(-3).AddMinutes(15), resolvedAt: now.AddDays(-2));
        ticket4.Survey = new SatisfactionSurvey
        {
            Id = Guid.NewGuid(), TicketId = ticket4.Id, Rating = 5,
            Observations = "Excelente atención, muy rápida la solución.", RecordedAt = now.AddDays(-2).AddMinutes(30)
        };
        db.Conversations.Add(c4);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Configuración a medida del negocio de demo.</summary>
    private static TenantConfiguration BuildConfiguration() => new()
    {
        CustomerAttention = new CustomerAttentionConfig
        {
            QueueWaitMessage = "Estamos conectándote con un asesor de AndinaShop, un momento por favor.",
            TransferMessage  = "Tu consulta se está transfiriendo al área indicada.",
            InactivityTimeoutMinutes = 20
        },
        SessionAssignment = new SessionAssignmentConfig
        {
            AssignmentMode = AssignmentMode.AutomaticBalanced,
            MaxConcurrentSessionsPerAgent = 4
        },
        SatisfactionSurvey = new SatisfactionSurveyConfig { SurveyEnabled = true, AllowObservations = true },
        BusinessHours = new BusinessHoursConfig
        {
            BusinessHoursEnabled = true,
            OutOfHoursMessage = "Nuestro horario es de lunes a sábado de 8:00 a 18:00. Déjanos tu mensaje y te contactaremos."
        },
        ChannelCustomization = new ChannelCustomizationConfig
        {
            WelcomeMessage = "¡Hola! Bienvenido a AndinaShop 🛒 ¿En qué podemos ayudarte hoy?",
            PrimaryColor   = "#2563eb"
        }
    };

    // ── Helpers de construcción del grafo ──────────────────────────────────────

    private static Conversation NewConversation(
        Guid tenantId, Channel channel, string externalUserId, string name,
        TicketStatus status, DateTime created, string? email = null, string? phone = null,
        bool authenticated = false, DateTime? resolved = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Channel = channel,
            ExternalUserId = externalUserId,
            ExternalUserName = name,
            CustomerEmail = email,
            CustomerPhone = phone,
            IsAuthenticated = authenticated,
            Status = status,
            CreatedAt = created,
            LastMessageAt = created,
            ResolvedAt = resolved
        };

    private static void AddMessage(Conversation c, MessageRole role, string content, DateTime at, Guid? senderId = null)
    {
        c.Messages.Add(new Message
        {
            Id = Guid.NewGuid(), ConversationId = c.Id, Role = role, Content = content, SenderId = senderId, CreatedAt = at
        });
        if (at > c.LastMessageAt) c.LastMessageAt = at;
    }

    private static Ticket AddTicket(
        Conversation c, Guid roleId, Guid? agentId, TicketStatus status, TicketPriority priority,
        DateTime? assignedAt = null, DateTime? firstResponseAt = null, DateTime? resolvedAt = null)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ConversationId = c.Id,
            AssignedRoleId = roleId,
            AssignedAgentId = agentId,
            Status = status,
            Priority = priority,
            CreatedAt = c.CreatedAt,
            AssignedAt = assignedAt,
            FirstResponseAt = firstResponseAt,
            ResolvedAt = resolvedAt
        };
        c.Ticket = ticket;
        return ticket;
    }

    [GeneratedRegex(@"[^a-z0-9_]")]
    private static partial Regex SafeSlugRegex();
}
