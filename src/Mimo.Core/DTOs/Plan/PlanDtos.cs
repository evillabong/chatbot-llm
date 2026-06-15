using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Plan;

/// <summary>Representación de un plan del catálogo.</summary>
public record PlanResponse(
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);

/// <summary>Datos para crear un plan. El código se normaliza a minúsculas.</summary>
public record CreatePlanRequest(
    [Required, MaxLength(50), RegularExpression(@"^[a-z0-9\-]+$",
        ErrorMessage = "El código solo puede contener minúsculas, números y guiones.")]
    string Code,
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description = null
);

/// <summary>Datos para actualizar un plan (el código es inmutable).</summary>
public record UpdatePlanRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    bool IsActive = true
);
