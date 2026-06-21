using System.ComponentModel.DataAnnotations;
using Mimo.Core.Automation;
using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Automation;

/// <summary>Regla de automatización (#24).</summary>
public record AutomationRuleResponse(
    Guid Id,
    string Name,
    string TriggerEvent,
    List<RuleCondition> Conditions,
    AutomationActionType ActionType,
    string ActionTaskTitle,
    Guid? ActionAssignedAgentId,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Crea una regla de automatización.</summary>
public record CreateAutomationRuleRequest(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(80)] string TriggerEvent,
    List<RuleCondition>? Conditions,
    [Required, MaxLength(200)] string ActionTaskTitle,
    Guid? ActionAssignedAgentId = null,
    bool IsEnabled = true
);

/// <summary>Actualiza una regla de automatización.</summary>
public record UpdateAutomationRuleRequest(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(80)] string TriggerEvent,
    List<RuleCondition>? Conditions,
    [Required, MaxLength(200)] string ActionTaskTitle,
    Guid? ActionAssignedAgentId,
    bool IsEnabled
);
