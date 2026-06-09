using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mimo.Infrastructure.Data.Factories;

/// <summary>
/// Factoría de diseño para TenantDbContext.
/// Requerida por las herramientas de EF Core (dotnet ef migrations add) en tiempo de diseño.
/// Las migraciones generadas representan el esquema base de cualquier tenant.
/// Lee la cadena de conexión desde la variable de entorno MIMO_CONNECTION_STRING,
/// o usa la conexión local por defecto para desarrollo.
/// </summary>
public class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MIMO_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=mimo;Username=mimo;Password=mimo";

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.UseVector());

        return new TenantDbContext(optionsBuilder.Options);
    }
}
