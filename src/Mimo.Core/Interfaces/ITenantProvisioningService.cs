using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Servicio responsable de provisionar el esquema de base de datos para un nuevo tenant.
/// Crea el esquema PostgreSQL y aplica las migraciones de TenantDbContext.
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>
    /// Aprovisiona el esquema del tenant: crea el esquema en PostgreSQL, ejecuta las migraciones
    /// y crea el rol "Administrador" junto con el funcionario administrador inicial.
    /// </summary>
    Task ProvisionAsync(Tenant tenant, TenantAdminSeed admin, CancellationToken ct = default);

    /// <summary>
    /// Elimina el esquema completo del tenant de la base de datos.
    /// </summary>
    Task DeprovisionAsync(string slug, CancellationToken ct = default);
}
