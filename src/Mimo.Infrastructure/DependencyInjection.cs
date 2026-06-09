using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mimo.Core.Interfaces;
using Mimo.Infrastructure.Data;
using Mimo.Infrastructure.Data.Repositories;

namespace Mimo.Infrastructure;

/// <summary>
/// Extensión para registrar todos los servicios de infraestructura en el contenedor DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra DbContext, repositorios y servicios de infraestructura.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Base de datos con pgvector
        services.AddDbContext<MimoDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.UseVector()));

        // Repositorios
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        return services;
    }

    /// <summary>
    /// Variante sin pgvector para Mimo.Admin.Api (opera solo sobre esquema public).
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
