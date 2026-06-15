namespace Mimo.Core.DTOs.Ai;

/// <summary>Resumen de uso de IA por tenant para un periodo, expuesto al SuperAdmin.</summary>
public record AiUsageSummaryResponse(
    Guid TenantId,
    int Requests,
    long PromptTokens,
    long CompletionTokens,
    long TotalTokens
);
