using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Tareas operativas (#24) por encima del cliente Kiota.
/// </summary>
public sealed class TasksService(MimoApiClient api)
{
    public async Task<List<WorkTaskResponse>> ListAsync(int? status = null, Guid? assignedAgentId = null, CancellationToken ct = default)
    {
        var result = await api.Tasks.GetAsync(rc =>
        {
            if (status is not null) rc.QueryParameters.Status = status;
            if (assignedAgentId is not null) rc.QueryParameters.AssignedAgentId = assignedAgentId;
        }, ct);
        return result ?? [];
    }

    public Task<WorkTaskResponse?> CreateAsync(CreateWorkTaskRequest request, CancellationToken ct = default) =>
        api.Tasks.PostAsync(request, cancellationToken: ct);

    public Task<WorkTaskResponse?> UpdateAsync(Guid id, UpdateWorkTaskRequest request, CancellationToken ct = default) =>
        api.Tasks.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        api.Tasks.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
