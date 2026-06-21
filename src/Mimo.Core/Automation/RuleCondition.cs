namespace Mimo.Core.Automation;

/// <summary>
/// Condición de una regla de automatización (#24): el campo del evento comparado con el valor según
/// <see cref="Operator"/>. Varias condiciones se combinan con AND. El operador por defecto es
/// <see cref="ConditionOperator.Equals"/> (las condiciones previas sin operador se interpretan así).
/// </summary>
public sealed record RuleCondition(string Field, string Value, ConditionOperator Operator = ConditionOperator.Equals);
