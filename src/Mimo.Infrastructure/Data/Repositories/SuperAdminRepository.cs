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

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => db.SuperAdmins.AnyAsync(ct);

    public async Task<SuperAdmin> AddAsync(SuperAdmin superAdmin, CancellationToken ct = default)
    {
        db.SuperAdmins.Add(superAdmin);
        await db.SaveChangesAsync(ct);
        return superAdmin;
    }
}
