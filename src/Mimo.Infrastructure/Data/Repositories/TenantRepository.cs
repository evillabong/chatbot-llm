using Microsoft.EntityFrameworkCore;
using Mimo.Core.Common;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Common;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de tenants sobre el esquema public (GlobalDbContext).
/// Este repositorio NUNCA debe usar TenantDbContext.
/// </summary>
public class TenantRepository(GlobalDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public Task<PagedResult<Tenant>> ListAsync(
        PaginationRequest pagination, bool? isActive, CancellationToken ct = default)
    {
        var query = db.Tenants.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        return query
            .OrderBy(t => t.Name)
            .ToPagedResultAsync(pagination, ct);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => db.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        return tenant;
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Update(tenant);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
