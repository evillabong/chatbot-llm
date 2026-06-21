using Microsoft.EntityFrameworkCore;
using Mimo.Core.Common;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Infrastructure.Common;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de funcionarios. Opera sobre el esquema del tenant activo (TenantDbContext).
/// </summary>
public class AgentRepository(TenantDbContext db) : IAgentRepository
{
    public Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default)
        => db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Email == email, ct);

    public async Task<IReadOnlyList<Agent>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var query = db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        return await query.OrderBy(a => a.FullName).ToListAsync(ct);
    }

    public Task<PagedResult<Agent>> ListPagedAsync(
        bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        return query.OrderBy(a => a.FullName).ToPagedResultAsync(page, pageSize, ct);
    }

    public async Task<IReadOnlyList<Agent>> GetByRoleAsync(Guid roleId, CancellationToken ct = default)
        => await db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .Where(a => a.AgentRoles.Any(ar => ar.RoleId == roleId) && a.IsActive)
            .OrderBy(a => a.FullName)
            .ToListAsync(ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => db.Agents.AnyAsync(a => a.Email == email, ct);

    public async Task<Agent> AddAsync(Agent agent, CancellationToken ct = default)
    {
        db.Agents.Add(agent);
        await db.SaveChangesAsync(ct);
        return agent;
    }

    public Task UpdateAsync(Agent agent, CancellationToken ct = default)
    {
        db.Agents.Update(agent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
