using Microsoft.EntityFrameworkCore;
using Mimo.Infrastructure.Data.Configurations.Global;

// Alias necesario: el nombre 'Tenant' es ambiguo porque existe el namespace hermano
// Mimo.Infrastructure.Data.Configurations.Tenant.
using TenantModel = Mimo.Core.Models.Tenant;

namespace Mimo.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos para el esquema público (global).
/// Solo gestiona la tabla de tenants; es el único contexto que no depende del esquema del tenant activo.
/// </summary>
public class GlobalDbContext(DbContextOptions<GlobalDbContext> options) : DbContext(options)
{
    public DbSet<TenantModel> Tenants => Set<TenantModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TenantEntityConfiguration());
    }
}
