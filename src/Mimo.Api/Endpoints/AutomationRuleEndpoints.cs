using System.Text.Json;
using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.Automation;
using Mimo.Core.DTOs.Automation;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Webhooks;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Gestión de reglas de automatización (#24, motor "evento → condición → acción"). Para TenantAdmin:
/// declarar el evento disparador, condiciones (AND sobre campos del evento) y la acción (hoy: crear
/// tarea). El dispatcher las ejecuta cuando ocurre el evento.
/// </summary>
public static class AutomationRuleEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapAutomationRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/automation-rules")
            .WithTags("AutomationRules")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListAutomationRules")
            .WithSummary("Lista las reglas de automatización del tenant.")
            .Produces<List<AutomationRuleResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateAutomationRule")
            .WithSummary("Crea una regla de automatización.")
            .Produces<AutomationRuleResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateAutomationRule")
            .WithSummary("Actualiza una regla de automatización (query: id).")
            .Produces<AutomationRuleResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/", DeleteAsync)
            .WithName("DeleteAutomationRule")
            .WithSummary("Elimina una regla de automatización (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(IAutomationRuleRepository repo, CancellationToken ct = default)
    {
        var items = await repo.ListAsync(ct);
        return Results.Ok(items.Select(ToResponse).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateAutomationRuleRequest request,
        IAutomationRuleRepository repo,
        HttpContext context,
        CancellationToken ct = default)
    {
        if (!WebhookEventTypes.All.Contains(request.TriggerEvent))
            return Results.BadRequest(new { error = "Evento disparador no soportado." });

        var rule = new AutomationRule
        {
            Id                    = Guid.NewGuid(),
            TenantId              = context.GetTenantId(),
            Name                  = request.Name.Trim(),
            TriggerEvent          = request.TriggerEvent,
            ConditionsJson        = SerializeConditions(request.Conditions),
            ActionType            = AutomationActionType.CreateTask,
            ActionTaskTitle       = request.ActionTaskTitle.Trim(),
            ActionAssignedAgentId = request.ActionAssignedAgentId,
            IsEnabled             = request.IsEnabled,
            CreatedAt             = DateTime.UtcNow
        };
        await repo.AddAsync(rule, ct);

        return Results.Created("/automation-rules", ToResponse(rule));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateAutomationRuleRequest request,
        IAutomationRuleRepository repo,
        CancellationToken ct = default)
    {
        var rule = await repo.GetByIdAsync(id, ct);
        if (rule is null)
            return Results.NotFound(new { error = "Regla no encontrada." });

        if (!WebhookEventTypes.All.Contains(request.TriggerEvent))
            return Results.BadRequest(new { error = "Evento disparador no soportado." });

        rule.Name                  = request.Name.Trim();
        rule.TriggerEvent          = request.TriggerEvent;
        rule.ConditionsJson        = SerializeConditions(request.Conditions);
        rule.ActionTaskTitle       = request.ActionTaskTitle.Trim();
        rule.ActionAssignedAgentId = request.ActionAssignedAgentId;
        rule.IsEnabled             = request.IsEnabled;
        rule.UpdatedAt             = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(rule));
    }

    private static async Task<IResult> DeleteAsync(Guid id, IAutomationRuleRepository repo, CancellationToken ct = default)
    {
        var rule = await repo.GetByIdAsync(id, ct);
        if (rule is null)
            return Results.NotFound(new { error = "Regla no encontrada." });

        await repo.RemoveAsync(rule, ct);
        return Results.NoContent();
    }

    private static string SerializeConditions(List<RuleCondition>? conditions)
    {
        var clean = (conditions ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Field))
            .Select(c => new RuleCondition(c.Field.Trim(), c.Value?.Trim() ?? ""))
            .ToList();
        return JsonSerializer.Serialize(clean, JsonOptions);
    }

    private static AutomationRuleResponse ToResponse(AutomationRule r)
    {
        List<RuleCondition> conditions;
        try { conditions = JsonSerializer.Deserialize<List<RuleCondition>>(r.ConditionsJson, JsonOptions) ?? []; }
        catch { conditions = []; }

        return new AutomationRuleResponse(
            r.Id, r.Name, r.TriggerEvent, conditions, r.ActionType, r.ActionTaskTitle,
            r.ActionAssignedAgentId, r.IsEnabled, r.CreatedAt, r.UpdatedAt);
    }
}
