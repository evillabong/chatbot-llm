namespace Mimo.Core.Automation;

/// <summary>
/// Operador de comparación de una condición de regla (#24). Se compara el dato del evento contra el
/// valor esperado (ignorando mayúsculas/espacios).
/// </summary>
public enum ConditionOperator
{
    /// <summary>El dato es igual al valor.</summary>
    Equals = 0,

    /// <summary>El dato es distinto del valor (un campo ausente cuenta como distinto).</summary>
    NotEquals = 1,

    /// <summary>El dato contiene el valor.</summary>
    Contains = 2,

    /// <summary>El dato no contiene el valor (un campo ausente cuenta como que no lo contiene).</summary>
    NotContains = 3
}
