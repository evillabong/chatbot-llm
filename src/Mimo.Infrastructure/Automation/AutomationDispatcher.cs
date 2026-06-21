using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mimo.Core.Automation;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Automation;

/// <summary>
/// Motor de reglas (#24): ante un evento, carga las reglas habilitadas para ese disparador, evalúa sus
/// condiciones contra los datos del evento y ejecuta la acción. Best-effort: nunca propaga (no debe
/// romper la operación que originó el evento). Opera en el esquema del tenant ya resuelto.
/// </summary>
public sealed class AutomationDispatcher(
    IAutomationRuleRepository rules,
    IWorkTaskRepository tasks,
    IMcpToolProvider mcp,
    ICrmSyncService crmSync,
    ILogger<AutomationDispatcher> logger) : IAutomationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task DispatchAsync(string eventType, object payload, CancellationToken ct = default)
    {
        try
        {
            var enabled = await rules.ListEnabledByTriggerAsync(eventType, ct);
            if (enabled.Count == 0)
                return;

            var data = Flatten(payload);

            foreach (var rule in enabled)
            {
                var conditions = DeserializeConditions(rule.ConditionsJson);
                if (!RuleEvaluator.Matches(conditions, data))
                    continue;

                await ExecuteAsync(rule, data, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo al evaluar reglas de automatización para el evento {EventType}.", eventType);
        }
    }

    private async Task ExecuteAsync(AutomationRule rule, IReadOnlyDictionary<string, string?> data, CancellationToken ct)
    {
        switch (rule.ActionType)
        {
            case AutomationActionType.CreateTask:
                var title = TemplateRenderer.Render(rule.ActionTaskTitle ?? "", data);
                var task = new WorkTask
                {
                    Id              = Guid.NewGuid(),
                    TenantId        = rule.TenantId,
                    Title           = string.IsNullOrWhiteSpace(title) ? rule.Name : title,
                    Description     = $"Creada por la regla de automatización «{rule.Name}» ({rule.TriggerEvent}).",
                    Status          = WorkTaskStatus.Pending,
                    AssignedAgentId = rule.ActionAssignedAgentId,
                    ConversationId  = TryGuid(data, "id", "conversationId"),
                    TicketId        = TryGuid(data, "ticketId"),
                    CreatedAt       = DateTime.UtcNow
                };
                await tasks.AddAsync(task, ct);
                logger.LogInformation("Automatización: regla {RuleId} creó la tarea {TaskId}.", rule.Id, task.Id);
                break;

            case AutomationActionType.Escalate:
                var conversationId = TryGuid(data, "id", "conversationId");
                if (conversationId is null)
                {
                    logger.LogWarning("Automatización: regla {RuleId} (Escalate) sin conversación en el evento {Trigger}; se omite.", rule.Id, rule.TriggerEvent);
                    break;
                }
                var reason = TemplateRenderer.Render(rule.ActionEscalateReason ?? "Escalada automática", data);
                await mcp.RequestHumanAgentAsync(conversationId.Value, reason, ct);
                logger.LogInformation("Automatización: regla {RuleId} escaló la conversación {ConversationId}.", rule.Id, conversationId);
                break;

            case AutomationActionType.SyncCrm:
                var opportunityId = TryGuid(data, "id", "opportunityId");
                if (opportunityId is null)
                {
                    logger.LogWarning("Automatización: regla {RuleId} (SyncCrm) sin oportunidad en el evento {Trigger}; se omite.", rule.Id, rule.TriggerEvent);
                    break;
                }
                await crmSync.SyncOpportunityAsync(opportunityId.Value, ct);
                logger.LogInformation("Automatización: regla {RuleId} sincronizó la oportunidad {OpportunityId} con el CRM.", rule.Id, opportunityId);
                break;

            default:
                logger.LogWarning("Automatización: acción no soportada {ActionType} (regla {RuleId}).", rule.ActionType, rule.Id);
                break;
        }
    }

    /// <summary>Aplana el payload (objeto anónimo) a un diccionario de strings por propiedad de primer nivel.</summary>
    private static Dictionary<string, string?> Flatten(object payload)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload, JsonOptions));
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Null   => null,
                        _                    => prop.Value.GetRawText()
                    };
                }
            }
        }
        catch { /* payload no serializable: queda vacío */ }
        return result;
    }

    private static List<RuleCondition> DeserializeConditions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<RuleCondition>>(json, JsonOptions) ?? []; }
        catch { return []; }
    }

    private static Guid? TryGuid(IReadOnlyDictionary<string, string?> data, params string[] keys)
    {
        foreach (var key in keys)
            if (data.TryGetValue(key, out var raw) && Guid.TryParse(raw, out var id))
                return id;
        return null;
    }
}
