using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de funcionarios. Opera sobre el esquema del tenant
/// establecido previamente por el TenantResolutionMiddleware.
/// </summary>
public class AgentRepository : IAgentRepository
{
    private readonly MimoDbContext _db;

    public AgentRepository(MimoDbContext db) => _db = db;

    public Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Email == email, ct);

    public async Task<IReadOnlyList<Agent>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var query = _db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        return await query.OrderBy(a => a.FullName).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Agent>> GetByRoleAsync(Guid roleId, CancellationToken ct = default)
        => await _db.Agents
            .Include(a => a.AgentRoles).ThenInclude(ar => ar.Role)
            .Where(a => a.AgentRoles.Any(ar => ar.RoleId == roleId) && a.IsActive)
            .OrderBy(a => a.FullName)
            .ToListAsync(ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => _db.Agents.AnyAsync(a => a.Email == email, ct);

    public async Task<Agent> AddAsync(Agent agent, CancellationToken ct = default)
    {
        _db.Agents.Add(agent);
        await _db.SaveChangesAsync(ct);
        return agent;
    }

    public Task UpdateAsync(Agent agent, CancellationToken ct = default)
    {
        _db.Agents.Update(agent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
