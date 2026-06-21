using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a las tareas operativas (#24) dentro del esquema de un tenant.
/// </summary>
public interface IWorkTaskRepository
{
    Task<WorkTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkTask>> ListAsync(WorkTaskStatus? status = null, Guid? assignedAgentId = null, CancellationToken ct = default);
    Task<WorkTask> AddAsync(WorkTask task, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task RemoveAsync(WorkTask task, CancellationToken ct = default);
}
