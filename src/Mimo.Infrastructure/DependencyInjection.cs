using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mimo.Core.Interfaces;
using Mimo.Infrastructure.AI;
using Mimo.Infrastructure.Data;
using Mimo.Infrastructure.Data.Repositories;
using Mimo.Infrastructure.Orchestration;
using Mimo.Infrastructure.Services;
using Mimo.Infrastructure.Ticketing;

namespace Mimo.Infrastructure;

/// <summary>
/// Extensión para registrar todos los servicios de infraestructura en el contenedor DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra ambos DbContext, repositorios, cliente LLM y servicios de dominio.
    /// Usado por Mimo.Api (tenant-facing).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        // Contexto global: solo esquema public (tabla tenants)
        services.AddDbContext<GlobalDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Contexto de tenant: todas las entidades del tenant activo; requiere pgvector
        services.AddDbContext<TenantDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        // Factory para TenantProvisioningService (necesita crear instancias en background)
        services.AddDbContextFactory<TenantDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()),
            ServiceLifetime.Scoped);

        // Repositorios
        services.AddScoped<ITenantRepository,          TenantRepository>();
        services.AddScoped<IAgentRepository,           AgentRepository>();
        services.AddScoped<IRoleRepository,            RoleRepository>();
        services.AddScoped<IDocumentRepository,        DocumentRepository>();
        services.AddScoped<IConversationRepository,    ConversationRepository>();

        // Servicios de dominio
        services.AddScoped<ITicketService,             TicketService>();
        services.AddScoped<IVectorSearchService,       VectorSearchService>();
        services.AddScoped<IConversationOrchestrator,  ConversationOrchestrator>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        // Cliente LLM (DeepSeek — compatible con OpenAI)
        services.AddHttpClient<ILlmClient, DeepSeekClient>(http =>
        {
            var baseUrl = configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";
            var apiKey  = configuration["DeepSeek:ApiKey"]  ?? string.Empty;
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        });

        return services;
    }

    /// <summary>
    /// Variante reducida para Mimo.Admin.Api.
    /// Registra GlobalDbContext (esquema public) y TenantDbContext (para provisionamiento).
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

        services.AddScoped<ITenantRepository,          TenantRepository>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        return services;
    }
}
