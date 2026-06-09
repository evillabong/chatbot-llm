using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a datos de super administradores (esquema public, GlobalDbContext).
/// </summary>
public interface ISuperAdminRepository
{
    Task<SuperAdmin?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<SuperAdmin> AddAsync(SuperAdmin superAdmin, CancellationToken ct = default);
}
