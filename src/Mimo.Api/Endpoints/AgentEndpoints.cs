using Mimo.Core.DTOs.Agent;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Endpoints de gestión de funcionarios. Requieren autenticación de TenantAdmin.
/// Operan sobre el esquema del tenant resuelto por TenantResolutionMiddleware.
/// </summary>
public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/agents")
            .WithTags("Agents")
            .RequireAuthorization();

        group.MapGet("/", ListAgentsAsync)
            .WithName("ListAgents")
            .WithSummary("Lista los funcionarios del tenant.");

        group.MapGet("/{id:guid}", GetAgentAsync)
            .WithName("GetAgent")
            .WithSummary("Obtiene un funcionario por su ID.");

        group.MapPost("/", CreateAgentAsync)
            .WithName("CreateAgent")
            .WithSummary("Crea un nuevo funcionario en el tenant.");

        group.MapPut("/{id:guid}", UpdateAgentAsync)
            .WithName("UpdateAgent")
            .WithSummary("Actualiza los datos de un funcionario.");

        group.MapDelete("/{id:guid}", DeactivateAgentAsync)
            .WithName("DeactivateAgent")
            .WithSummary("Desactiva un funcionario (borrado lógico).");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> ListAgentsAsync(
        IAgentRepository repo,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var agents = await repo.ListAsync(isActive, ct);
        return Results.Ok(agents.Select(ToResponse).ToList());
    }

    private static async Task<IResult> GetAgentAsync(
        Guid id, IAgentRepository repo, CancellationToken ct = default)
    {
        var agent = await repo.GetByIdAsync(id, ct);
        return agent is null
            ? Results.NotFound(new { error = "Funcionario no encontrado." })
            : Results.Ok(ToResponse(agent));
    }

    private static async Task<IResult> CreateAgentAsync(
        CreateAgentRequest request,
        IAgentRepository repo,
        IRoleRepository roleRepo,
        HttpContext context,
        CancellationToken ct = default)
    {
        if (await repo.EmailExistsAsync(request.Email, ct))
            return Results.Conflict(new { error = $"El email '{request.Email}' ya está registrado." });

        var tenantId = (Guid)context.Items["TenantId"]!;

        var agent = new Agent
        {
            Id                    = Guid.NewGuid(),
            TenantId              = tenantId,
            Email                 = request.Email,
            FullName              = request.FullName,
            Alias                 = request.Alias,
            MaxConcurrentSessions = request.MaxConcurrentSessions,
            IsActive              = true,
            CreatedAt             = DateTime.UtcNow
        };

        // Vincular roles si se proporcionaron
        if (request.RoleIds?.Count > 0)
        {
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepo.GetByIdAsync(roleId, ct);
                if (role is null)
                    return Results.BadRequest(new { error = $"Rol con ID '{roleId}' no encontrado." });

                agent.AgentRoles.Add(new AgentRole { AgentId = agent.Id, RoleId = roleId });
            }
        }

        await repo.AddAsync(agent, ct);
        return Results.Created($"/agents/{agent.Id}", ToResponse(agent));
    }

    private static async Task<IResult> UpdateAgentAsync(
        Guid id,
        UpdateAgentRequest request,
        IAgentRepository repo,
        IRoleRepository roleRepo,
        CancellationToken ct = default)
    {
        var agent = await repo.GetByIdAsync(id, ct);
        if (agent is null)
            return Results.NotFound(new { error = "Funcionario no encontrado." });

        agent.FullName              = request.FullName;
        agent.Alias                 = request.Alias;
        agent.MaxConcurrentSessions = request.MaxConcurrentSessions;
        agent.IsActive              = request.IsActive;

        // Reemplazar roles si se proporcionaron
        if (request.RoleIds is not null)
        {
            agent.AgentRoles.Clear();
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepo.GetByIdAsync(roleId, ct);
                if (role is null)
                    return Results.BadRequest(new { error = $"Rol con ID '{roleId}' no encontrado." });

                agent.AgentRoles.Add(new AgentRole { AgentId = agent.Id, RoleId = roleId });
            }
        }

        await repo.UpdateAsync(agent, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(agent));
    }

    private static async Task<IResult> DeactivateAgentAsync(
        Guid id, IAgentRepository repo, CancellationToken ct = default)
    {
        var agent = await repo.GetByIdAsync(id, ct);
        if (agent is null)
            return Results.NotFound(new { error = "Funcionario no encontrado." });

        agent.IsActive = false;
        await repo.UpdateAsync(agent, ct);
        await repo.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static AgentResponse ToResponse(Agent a) =>
        new(a.Id, a.Email, a.FullName, a.Alias, a.IsActive,
            a.MaxConcurrentSessions,
            a.AgentRoles.Select(ar => new RoleSummary(ar.RoleId, ar.Role?.Name ?? "")).ToList(),
            a.CreatedAt);
}
