namespace Mimo.Core.Automation;

/// <summary>
/// Condición simple de una regla de automatización (#24): el campo del evento debe ser igual al valor.
/// Varias condiciones se combinan con AND.
/// </summary>
public sealed record RuleCondition(string Field, string Value);
