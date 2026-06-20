using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Ai;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using PlanModel = Mimo.Core.Models.Plan;

namespace Mimo.Admin.Api.Endpoints;

/// <summary>
/// Administración de las políticas de IA por plan (modelos permitidos y cuotas). Solo SuperAdmin.
/// </summary>
public static class AiPlanPolicyEndpoints
{
    public static IEndpointRouteBuilder MapAiPlanPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ai/plan-policies")
            .WithTags("AiPlanPolicies")
            .RequireAuthorization(MimoAuthorization.Policies.SuperAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListAiPlanPolicies").WithSummary("Lista las políticas de IA por plan.")
            .Produces<List<AiPlanPolicyResponse>>();

        group.MapGet("/detail", GetAsync)
            .WithName("GetAiPlanPolicy").WithSummary("Obtiene la política de un plan (query: planCode).")
            .Produces<AiPlanPolicyResponse>().Produces(StatusCodes.Status404NotFound);

        group.MapPut("/", UpsertAsync)
            .WithName("UpsertAiPlanPolicy").WithSummary("Crea o actualiza la política de IA de un plan (query: planCode).")
            .Produces<AiPlanPolicyResponse>().Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/", DeleteAsync)
            .WithName("DeleteAiPlanPolicy").WithSummary("Elimina la política de IA de un plan (query: planCode).")
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(IAiPlanPolicyRepository repo, CancellationToken ct = default)
        => Results.Ok((await repo.ListAsync(ct)).Select(ToResponse).ToList());

    private static async Task<IResult> GetAsync(string planCode, IAiPlanPolicyRepository repo, CancellationToken ct = default)
    {
        var policy = await repo.GetByPlanCodeAsync(planCode, ct);
        return policy is null ? Results.NotFound(new { error = "Política no encontrada." }) : Results.Ok(ToResponse(policy));
    }

    private static async Task<IResult> UpsertAsync(
        string planCode,
        UpsertAiPlanPolicyRequest request,
        IAiPlanPolicyRepository repo,
        IPlanRepository planRepo,
        IAiConnectorRepository connectorRepo,
        CancellationToken ct = default)
    {
        var code = PlanModel.NormalizeCode(planCode);

        // La política debe referenciar un plan existente (la FK lo respalda; validamos para un 400 claro).
        if (!await planRepo.ExistsAsync(code, ct))
            return Results.BadRequest(new { error = $"El plan '{code}' no existe o está inactivo." });

        var providers = request.AllowedProviders
            .Select(p => p.Trim().ToLowerInvariant())
            .Where(p => p.Length > 0)
            .Distinct()
            .ToList();

        // Validar que cada proveedor permitido tenga un conector de IA configurado (#11): así una
        // política no habilita un proveedor que el gateway no podría resolver.
        if (providers.Count > 0)
        {
            var known = (await connectorRepo.ListAsync(ct))
                .Select(c => c.Provider.ToLowerInvariant())
                .ToHashSet();
            var unknown = providers.Where(p => !known.Contains(p)).ToList();
            if (unknown.Count > 0)
                return Results.BadRequest(new { error = $"Proveedores sin conector configurado: {string.Join(", ", unknown)}." });
        }

        var existing = await repo.GetByPlanCodeAsync(code, ct);
        if (existing is null)
        {
            var policy = new AiPlanPolicy
            {
                Id                  = Guid.NewGuid(),
                PlanCode            = code,
                AllowedProviders    = providers,
                MonthlyRequestQuota = request.MonthlyRequestQuota,
                MonthlyTokenQuota   = request.MonthlyTokenQuota,
                IsActive            = request.IsActive,
                CreatedAt           = DateTime.UtcNow
            };
            await repo.AddAsync(policy, ct);
            return Results.Created($"/ai/plan-policies/detail?planCode={code}", ToResponse(policy));
        }

        existing.AllowedProviders    = providers;
        existing.MonthlyRequestQuota = request.MonthlyRequestQuota;
        existing.MonthlyTokenQuota   = request.MonthlyTokenQuota;
        existing.IsActive            = request.IsActive;
        existing.UpdatedAt           = DateTime.UtcNow;

        await repo.UpdateAsync(existing, ct);
        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(existing));
    }

    private static async Task<IResult> DeleteAsync(string planCode, IAiPlanPolicyRepository repo, CancellationToken ct = default)
    {
        var policy = await repo.GetByPlanCodeAsync(planCode, ct);
        if (policy is null) return Results.NotFound(new { error = "Política no encontrada." });

        await repo.DeleteAsync(policy, ct);
        await repo.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static AiPlanPolicyResponse ToResponse(AiPlanPolicy p) =>
        new(p.PlanCode, p.AllowedProviders, p.MonthlyRequestQuota, p.MonthlyTokenQuota, p.IsActive);
}
