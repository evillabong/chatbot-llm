using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Tasks;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Tasks;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Tareas operativas (#24, automatización — entidad Tarea). Gestión para cualquier funcionario:
/// crear, listar (filtro por estado/responsable), actualizar (incluido el estado, que fija/limpia la
/// fecha de cierre) y eliminar. Base sobre la que el motor de reglas creará tareas como acción.
/// </summary>
public static class WorkTaskEndpoints
{
    public static IEndpointRouteBuilder MapWorkTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks")
            .WithTags("Tasks")
            .RequireAuthorization(MimoAuthorization.Policies.Agent);

        group.MapGet("/", ListAsync)
            .WithName("ListTasks")
            .WithSummary("Lista las tareas (query opcional: status, assignedAgentId).")
            .Produces<List<WorkTaskResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateTask")
            .WithSummary("Crea una tarea.")
            .Produces<WorkTaskResponse>(StatusCodes.Status201Created);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateTask")
            .WithSummary("Actualiza una tarea, incluido su estado (query: id).")
            .Produces<WorkTaskResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/", DeleteAsync)
            .WithName("DeleteTask")
            .WithSummary("Elimina una tarea (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IWorkTaskRepository repo, int? status = null, Guid? assignedAgentId = null, Guid? opportunityId = null, CancellationToken ct = default)
    {
        WorkTaskStatus? filter = status is not null && Enum.IsDefined(typeof(WorkTaskStatus), status.Value)
            ? (WorkTaskStatus)status.Value
            : null;

        var items = await repo.ListAsync(filter, assignedAgentId, opportunityId, ct);
        return Results.Ok(items.Select(ToResponse).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkTaskRequest request, IWorkTaskRepository repo, HttpContext context, CancellationToken ct = default)
    {
        var task = new WorkTask
        {
            Id              = Guid.NewGuid(),
            TenantId        = context.GetTenantId(),
            Title           = request.Title.Trim(),
            Description     = request.Description,
            Status          = WorkTaskStatus.Pending,
            AssignedAgentId = request.AssignedAgentId,
            DueAt           = request.DueAt,
            ConversationId  = request.ConversationId,
            TicketId        = request.TicketId,
            OpportunityId   = request.OpportunityId,
            CreatedAt       = DateTime.UtcNow
        };
        await repo.AddAsync(task, ct);
        return Results.Created("/tasks", ToResponse(task));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateWorkTaskRequest request, IWorkTaskRepository repo, CancellationToken ct = default)
    {
        var task = await repo.GetByIdAsync(id, ct);
        if (task is null)
            return Results.NotFound(new { error = "Tarea no encontrada." });

        task.Title           = request.Title.Trim();
        task.Description     = request.Description;
        task.Status          = request.Status;
        task.AssignedAgentId = request.AssignedAgentId;
        task.DueAt           = request.DueAt;
        task.CompletedAt     = WorkTaskStatusRules.ResolveCompletedAt(request.Status, task.CompletedAt, DateTime.UtcNow);
        task.UpdatedAt       = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> DeleteAsync(Guid id, IWorkTaskRepository repo, CancellationToken ct = default)
    {
        var task = await repo.GetByIdAsync(id, ct);
        if (task is null)
            return Results.NotFound(new { error = "Tarea no encontrada." });

        await repo.RemoveAsync(task, ct);
        return Results.NoContent();
    }

    private static WorkTaskResponse ToResponse(WorkTask t) => new(
        t.Id, t.Title, t.Description, t.Status, t.AssignedAgentId, t.DueAt,
        t.ConversationId, t.TicketId, t.OpportunityId, t.CreatedAt, t.UpdatedAt, t.CompletedAt);
}
