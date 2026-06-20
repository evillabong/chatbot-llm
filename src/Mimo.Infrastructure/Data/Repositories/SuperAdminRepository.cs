using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de super administradores. Opera sobre el esquema public (GlobalDbContext).
/// </summary>
public class SuperAdminRepository(GlobalDbContext db) : ISuperAdminRepository
{
    public Task<SuperAdmin?> GetByEmailAsync(string email, CancellationToken ct = default)
        => db.SuperAdmins.FirstOrDefaultAsync(s => s.Email == email, ct);

    public Task<SuperAdmin?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.SuperAdmins.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => db.SuperAdmins.AnyAsync(ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => db.SuperAdmins.AnyAsync(s => s.Email == email, ct);

    public Task<int> CountActiveAsync(CancellationToken ct = default)
        => db.SuperAdmins.CountAsync(s => s.IsActive, ct);

    public async Task<IReadOnlyList<SuperAdmin>> ListAsync(CancellationToken ct = default)
        => await db.SuperAdmins.AsNoTracking().OrderBy(s => s.Email).ToListAsync(ct);

    public async Task<SuperAdmin> AddAsync(SuperAdmin superAdmin, CancellationToken ct = default)
    {
        db.SuperAdmins.Add(superAdmin);
        await db.SaveChangesAsync(ct);
        return superAdmin;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
