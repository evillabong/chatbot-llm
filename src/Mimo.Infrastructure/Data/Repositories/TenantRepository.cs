using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de tenants sobre el esquema public.
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly MimoDbContext _db;

    public TenantRepository(MimoDbContext db) => _db = db;

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public async Task<(IReadOnlyList<Tenant> Items, int Total)> ListAsync(
        int page, int pageSize, bool? isActive, CancellationToken ct = default)
    {
        var query = _db.Tenants.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => _db.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Update(tenant);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
