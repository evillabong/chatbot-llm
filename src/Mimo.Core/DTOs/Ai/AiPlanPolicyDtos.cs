using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Ai;

/// <summary>Representación de la política de IA de un plan.</summary>
public record AiPlanPolicyResponse(
    string PlanCode,
    IReadOnlyList<string> AllowedProviders,
    int MonthlyRequestQuota,
    long MonthlyTokenQuota,
    bool IsActive
);

/// <summary>
/// Datos para crear o actualizar la política de IA de un plan (upsert por planCode en query).
/// Cuotas en 0 = ilimitado.
/// </summary>
public record UpsertAiPlanPolicyRequest(
    IReadOnlyList<string> AllowedProviders,
    [Range(0, int.MaxValue)] int MonthlyRequestQuota = 0,
    [Range(0, long.MaxValue)] long MonthlyTokenQuota = 0,
    bool IsActive = true
);
