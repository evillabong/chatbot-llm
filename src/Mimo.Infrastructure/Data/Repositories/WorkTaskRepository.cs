using Microsoft.EntityFrameworkCore;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de tareas operativas (#24) dentro del esquema de un tenant.
/// </summary>
public class WorkTaskRepository(TenantDbContext db) : IWorkTaskRepository
{
    public Task<WorkTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<WorkTask>> ListAsync(
        WorkTaskStatus? status = null, Guid? assignedAgentId = null, Guid? opportunityId = null, CancellationToken ct = default)
    {
        var query = db.Tasks.AsNoTracking().AsQueryable();
        if (status is not null)
            query = query.Where(t => t.Status == status.Value);
        if (assignedAgentId is not null)
            query = query.Where(t => t.AssignedAgentId == assignedAgentId.Value);
        if (opportunityId is not null)
            query = query.Where(t => t.OpportunityId == opportunityId.Value);
        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<WorkTask> AddAsync(WorkTask task, CancellationToken ct = default)
    {
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task RemoveAsync(WorkTask task, CancellationToken ct = default)
    {
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }
}
