using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mimo.Core.Interfaces;
using Mimo.Infrastructure.AI;
using Mimo.Infrastructure.Data;
using Mimo.Infrastructure.Data.Repositories;
using Mimo.Infrastructure.Orchestration;
using Mimo.Infrastructure.Ticketing;

namespace Mimo.Infrastructure;

/// <summary>
/// Extensión para registrar todos los servicios de infraestructura en el contenedor DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra DbContext (con pgvector), repositorios, cliente LLM y servicios de dominio.
    /// Usado por Mimo.Api (tenant-facing).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Base de datos con soporte pgvector
        services.AddDbContext<MimoDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.UseVector()));

        // Repositorios
        services.AddScoped<ITenantRepository,      TenantRepository>();
        services.AddScoped<IAgentRepository,       AgentRepository>();
        services.AddScoped<IRoleRepository,        RoleRepository>();
        services.AddScoped<IDocumentRepository,    DocumentRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();

        // Servicios de dominio
        services.AddScoped<ITicketService,              TicketService>();
        services.AddScoped<IVectorSearchService,        VectorSearchService>();
        services.AddScoped<IConversationOrchestrator,   ConversationOrchestrator>();

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
    /// Variante reducida sin pgvector para Mimo.Admin.Api (opera solo sobre esquema public).
    /// </summary>
    public static IServiceCollection AddAdminInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MimoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }
}
