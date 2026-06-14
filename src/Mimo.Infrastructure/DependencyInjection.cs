using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mimo.Core.Interfaces;
using Mimo.Core.Models.Configuration;
using Mimo.Infrastructure.AI;
using Mimo.Infrastructure.Assignment;
using Mimo.Infrastructure.Channels;
using Mimo.Infrastructure.Chat;
using Mimo.Infrastructure.Data;
using Mimo.Infrastructure.Data.Repositories;
using Mimo.Infrastructure.Mcp;
using Mimo.Infrastructure.Orchestration;
using Mimo.Infrastructure.Queuing;
using Mimo.Infrastructure.Services;
using Mimo.Infrastructure.Ticketing;
namespace Mimo.Infrastructure;

/// <summary>
/// Extensión para registrar todos los servicios de infraestructura en el contenedor DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra DbContexts, Redis, repositorios, cliente LLM y todos los servicios de dominio.
    /// Usado por Mimo.Api (tenant-facing).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        // ── Base de datos ──────────────────────────────────────────────────────
        services.AddDbContext<GlobalDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContext<TenantDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddDbContextFactory<TenantDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()),
            ServiceLifetime.Scoped);

        // ── Caché en memoria ──────────────────────────────────────────────────
        // Usado para cachear lookups frecuentes (tenant por slug, configuración).
        // No requiere persistencia; se invalida al reiniciar el proceso.
        services.AddMemoryCache();

        // ── Autenticación ───────────────────────────────────────────────────────
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // ── Repositorios ───────────────────────────────────────────────────────
        services.AddScoped<ITenantRepository,          TenantRepository>();
        services.AddScoped<IAgentRepository,           AgentRepository>();
        services.AddScoped<IRoleRepository,            RoleRepository>();
        services.AddScoped<IDocumentRepository,        DocumentRepository>();
        services.AddScoped<IConversationRepository,    ConversationRepository>();

        // ── Servicios de dominio ───────────────────────────────────────────────
        services.AddScoped<ITicketService,             TicketService>();
        services.AddScoped<ITicketQueueService,        TicketQueueService>();
        services.AddScoped<IAgentAssignmentService,    AgentAssignmentService>();
        services.AddScoped<IVectorSearchService,       VectorSearchService>();
        services.AddScoped<IInternalChatService,       InternalChatService>();
        services.AddScoped<IMcpToolProvider,           McpToolProvider>();
        services.AddScoped<IConversationOrchestrator,  ConversationOrchestrator>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        // ── Conectores de IA (abstracción ILlmClient + fábrica por proveedor) ─────
        // La configuración de cada IA vive en la BD (tabla ai_connectors, JSON).
        // LlmClientFactory resuelve el conector activo y construye el cliente concreto;
        // cambiar de proveedor es un cambio de datos, no de código.
        services.AddHttpClient(LlmClientFactory.HttpClientName);
        services.AddScoped<ILlmClientFactory, LlmClientFactory>();

        // ── Conectores de canales externos (keyed DI) ─────────────────────────
        // WebChatConnector se registra en Mimo.Api (requiere IHubContext<ChatHub>).
        services.AddKeyedScoped<IChannelConnector, FacebookConnector>("facebook");
        services.AddKeyedScoped<IChannelConnector, WhatsAppConnector>("whatsapp");
        services.AddKeyedScoped<IChannelConnector, TelegramConnector>("telegram");
        services.AddKeyedScoped<IChannelConnector, InstagramConnector>("instagram");

        return services;
    }

    /// <summary>
    /// Variante reducida para Mimo.Admin.Api (esquema public + aprovisionamiento).
    /// </summary>
    public static IServiceCollection AddAdminInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<GlobalDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContext<TenantDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddDbContextFactory<TenantDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()),
            ServiceLifetime.Scoped);

        // ── Autenticación ───────────────────────────────────────────────────────
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<ITenantRepository,          TenantRepository>();
        services.AddScoped<ISuperAdminRepository,      SuperAdminRepository>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        return services;
    }
}
