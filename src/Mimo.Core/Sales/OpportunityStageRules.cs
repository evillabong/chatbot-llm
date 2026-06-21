using Mimo.Core.Enums;

namespace Mimo.Core.Sales;

/// <summary>
/// Reglas puras del pipeline de oportunidades (#26). Sin dependencias, para probarse en aislamiento y
/// compartir la lógica entre el endpoint y futuros consumidores (automatización, reportes).
/// </summary>
public static class OpportunityStageRules
{
    /// <summary>True si la etapa cierra la oportunidad (Won/Lost).</summary>
    public static bool IsClosed(OpportunityStage stage)
        => stage is OpportunityStage.Won or OpportunityStage.Lost;

    /// <summary>
    /// Calcula el <c>ClosedAt</c> al cambiar de etapa: se fija al entrar a una etapa terminal (si no
    /// estaba ya marcado) y se limpia al reabrir.
    /// </summary>
    public static DateTime? ResolveClosedAt(OpportunityStage newStage, DateTime? existingClosedAt, DateTime now)
        => IsClosed(newStage) ? existingClosedAt ?? now : null;
}
