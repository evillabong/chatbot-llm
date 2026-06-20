using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a datos de super administradores (esquema public, GlobalDbContext).
/// </summary>
public interface ISuperAdminRepository
{
    Task<SuperAdmin?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<SuperAdmin?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<int> CountActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SuperAdmin>> ListAsync(CancellationToken ct = default);
    Task<SuperAdmin> AddAsync(SuperAdmin superAdmin, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
