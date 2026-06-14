using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Role;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Endpoints de gestión de roles/departamentos del tenant.
/// Requieren el rol Administrador (política TenantAdmin).
/// </summary>
public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles")
            .WithTags("Roles")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/", ListRolesAsync)
            .WithName("ListRoles")
            .WithSummary("Lista los roles/departamentos del tenant.");

        group.MapGet("/detail", GetRoleAsync)
            .WithName("GetRole")
            .WithSummary("Obtiene un rol por su ID (query: id).");

        group.MapPost("/", CreateRoleAsync)
            .WithName("CreateRole")
            .WithSummary("Crea un nuevo rol/departamento.");

        group.MapPut("/", UpdateRoleAsync)
            .WithName("UpdateRole")
            .WithSummary("Actualiza los datos de un rol (query: id).");

        group.MapDelete("/", DeactivateRoleAsync)
            .WithName("DeactivateRole")
            .WithSummary("Desactiva un rol / borrado lógico (query: id).");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> ListRolesAsync(
        IRoleRepository repo,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var roles = await repo.ListAsync(isActive, ct);
        return Results.Ok(roles.Select(ToResponse).ToList());
    }

    private static async Task<IResult> GetRoleAsync(
        Guid id, IRoleRepository repo, CancellationToken ct = default)
    {
        var role = await repo.GetByIdAsync(id, ct);
        return role is null
            ? Results.NotFound(new { error = "Rol no encontrado." })
            : Results.Ok(ToResponse(role));
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateRoleRequest request,
        IRoleRepository repo,
        HttpContext context,
        CancellationToken ct = default)
    {
        if (await repo.NameExistsAsync(request.Name, ct))
            return Results.Conflict(new { error = $"Ya existe un rol con el nombre '{request.Name}'." });

        var tenantId = (Guid)context.Items["TenantId"]!;

        var role = new Role
        {
            Id                = Guid.NewGuid(),
            TenantId          = tenantId,
            Name              = request.Name,
            Description       = request.Description,
            PriorityLevel     = request.PriorityLevel,
            CanViewAllTickets = request.CanViewAllTickets,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow
        };

        await repo.AddAsync(role, ct);
        return Results.Created($"/roles/detail?id={role.Id}", ToResponse(role));
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid id,
        UpdateRoleRequest request,
        IRoleRepository repo,
        CancellationToken ct = default)
    {
        var role = await repo.GetByIdAsync(id, ct);
        if (role is null)
            return Results.NotFound(new { error = "Rol no encontrado." });

        role.Name              = request.Name;
        role.Description       = request.Description;
        role.PriorityLevel     = request.PriorityLevel;
        role.CanViewAllTickets = request.CanViewAllTickets;
        role.IsActive          = request.IsActive;

        await repo.UpdateAsync(role, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(role));
    }

    private static async Task<IResult> DeactivateRoleAsync(
        Guid id, IRoleRepository repo, CancellationToken ct = default)
    {
        var role = await repo.GetByIdAsync(id, ct);
        if (role is null)
            return Results.NotFound(new { error = "Rol no encontrado." });

        role.IsActive = false;
        await repo.UpdateAsync(role, ct);
        await repo.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static RoleResponse ToResponse(Role r) =>
        new(r.Id, r.Name, r.Description, r.PriorityLevel, r.CanViewAllTickets, r.IsActive, r.CreatedAt);
}
