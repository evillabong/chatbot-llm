namespace Mimo.Core.Models;

/// <summary>
/// Catálogo de planes de la plataforma (esquema public, gestionado por el SuperAdmin).
///
/// El código del plan (<see cref="Code"/>) es la clave estable a la que referencian
/// <see cref="Tenant.Plan"/> y <see cref="AiPlanPolicy.PlanCode"/> mediante claves foráneas.
/// Los códigos se almacenan en minúsculas canónicas para evitar desajustes por mayúsculas.
/// </summary>
public class Plan
{
    public Guid Id { get; set; }

    /// <summary>Código único y estable del plan (minúsculas, ej: "free", "pro").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre visible del plan.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Normaliza un código de plan a su forma canónica (minúsculas, sin espacios).</summary>
    public static string NormalizeCode(string code) => code.Trim().ToLowerInvariant();
}
