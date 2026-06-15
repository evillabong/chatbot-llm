using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Plan;
using Mimo.Core.Interfaces;
using PlanModel = Mimo.Core.Models.Plan;

namespace Mimo.Admin.Api.Endpoints;

/// <summary>
/// Administración del catálogo de planes. Solo SuperAdmin.
/// Sin parámetros en la ruta (ADR 0006): el código va por query.
/// </summary>
public static class PlanEndpoints
{
    public static IEndpointRouteBuilder MapPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/plans")
            .WithTags("Plans")
            .RequireAuthorization(MimoAuthorization.Policies.SuperAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListPlans").WithSummary("Lista los planes del catálogo.");

        group.MapGet("/detail", GetAsync)
            .WithName("GetPlan").WithSummary("Obtiene un plan por código (query: code).");

        group.MapPost("/", CreateAsync)
            .WithName("CreatePlan").WithSummary("Crea un plan en el catálogo.");

        group.MapPut("/", UpdateAsync)
            .WithName("UpdatePlan").WithSummary("Actualiza nombre, descripción y estado de un plan (query: code).");

        group.MapDelete("/", DeactivateAsync)
            .WithName("DeactivatePlan").WithSummary("Desactiva un plan / borrado lógico (query: code).");

        return app;
    }

    private static async Task<IResult> ListAsync(IPlanRepository repo, bool? isActive = null, CancellationToken ct = default)
        => Results.Ok((await repo.ListAsync(isActive, ct)).Select(ToResponse).ToList());

    private static async Task<IResult> GetAsync(string code, IPlanRepository repo, CancellationToken ct = default)
    {
        var plan = await repo.GetByCodeAsync(code, ct);
        return plan is null ? Results.NotFound(new { error = "Plan no encontrado." }) : Results.Ok(ToResponse(plan));
    }

    private static async Task<IResult> CreateAsync(CreatePlanRequest request, IPlanRepository repo, CancellationToken ct = default)
    {
        var code = PlanModel.NormalizeCode(request.Code);
        if (await repo.GetByCodeAsync(code, ct) is not null)
            return Results.Conflict(new { error = $"Ya existe un plan con el código '{code}'." });

        var plan = new PlanModel
        {
            Id = Guid.NewGuid(), Code = code, Name = request.Name,
            Description = request.Description, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(plan, ct);
        return Results.Created($"/plans/detail?code={code}", ToResponse(plan));
    }

    private static async Task<IResult> UpdateAsync(string code, UpdatePlanRequest request, IPlanRepository repo, CancellationToken ct = default)
    {
        var plan = await repo.GetByCodeAsync(code, ct);
        if (plan is null) return Results.NotFound(new { error = "Plan no encontrado." });

        plan.Name        = request.Name;
        plan.Description = request.Description;
        plan.IsActive    = request.IsActive;
        plan.UpdatedAt   = DateTime.UtcNow;

        await repo.UpdateAsync(plan, ct);
        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(plan));
    }

    private static async Task<IResult> DeactivateAsync(string code, IPlanRepository repo, CancellationToken ct = default)
    {
        var plan = await repo.GetByCodeAsync(code, ct);
        if (plan is null) return Results.NotFound(new { error = "Plan no encontrado." });

        plan.IsActive  = false;
        plan.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(plan, ct);
        await repo.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static PlanResponse ToResponse(PlanModel p) =>
        new(p.Code, p.Name, p.Description, p.IsActive, p.CreatedAt);
}
