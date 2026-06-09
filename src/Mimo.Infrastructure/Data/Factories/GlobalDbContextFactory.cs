using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mimo.Infrastructure.Data.Factories;

/// <summary>
/// Factoría de diseño para GlobalDbContext.
/// Requerida por las herramientas de EF Core (dotnet ef migrations add) en tiempo de diseño.
/// Lee la cadena de conexión desde la variable de entorno MIMO_CONNECTION_STRING,
/// o usa la conexión local por defecto para desarrollo.
/// </summary>
public class GlobalDbContextFactory : IDesignTimeDbContextFactory<GlobalDbContext>
{
    public GlobalDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MIMO_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=mimo;Username=mimo;Password=mimo";

        var optionsBuilder = new DbContextOptionsBuilder<GlobalDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new GlobalDbContext(optionsBuilder.Options);
    }
}
