using Microsoft.EntityFrameworkCore;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data.Configurations.Global;

// Alias necesario: el nombre 'Tenant' es ambiguo porque existe el namespace hermano
// Mimo.Infrastructure.Data.Configurations.Tenant.
using TenantModel = Mimo.Core.Models.Tenant;

namespace Mimo.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos para el esquema público (global).
/// Gestiona las tablas de tenants y super administradores; es el único contexto
/// que no depende del esquema del tenant activo.
/// </summary>
public class GlobalDbContext(DbContextOptions<GlobalDbContext> options) : DbContext(options)
{
    public DbSet<TenantModel> Tenants => Set<TenantModel>();

    public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();

    public DbSet<AiConnector> AiConnectors => Set<AiConnector>();

    public DbSet<AiPlanPolicy> AiPlanPolicies => Set<AiPlanPolicy>();

    public DbSet<AiUsageRecord> AiUsageRecords => Set<AiUsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TenantEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SuperAdminEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AiConnectorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AiPlanPolicyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AiUsageRecordEntityConfiguration());
    }
}
