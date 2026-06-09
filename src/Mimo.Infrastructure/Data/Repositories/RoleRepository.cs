using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de roles/departamentos de un tenant.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly MimoDbContext _db;

    public RoleRepository(MimoDbContext db) => _db = db;

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Role>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var query = _db.Roles.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);
        return await query.OrderBy(r => r.PriorityLevel).ThenBy(r => r.Name).ToListAsync(ct);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
        => _db.Roles.AnyAsync(r => r.Name == name, ct);

    public async Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Update(role);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
