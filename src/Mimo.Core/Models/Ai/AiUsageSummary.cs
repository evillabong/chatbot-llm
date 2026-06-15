namespace Mimo.Core.Models.Ai;

/// <summary>
/// Resumen agregado de uso de IA de un tenant en un periodo, para las estadísticas del SuperAdmin.
/// </summary>
public record AiUsageSummary(
    Guid TenantId,
    int Requests,
    long PromptTokens,
    long CompletionTokens,
    long TotalTokens
);
