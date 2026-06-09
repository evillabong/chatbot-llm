using Mimo.Core.Models.Configuration;

namespace Mimo.Core.Models;

/// <summary>
/// Representa una entidad/organización registrada en la plataforma MIMO.
/// Cada tenant tiene su propio esquema de base de datos aislado.
/// </summary>
public class Tenant
{
    /// <summary>Identificador único del tenant.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre visible de la entidad.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Identificador corto único usado para el esquema de BD y resolución de tenant.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Indica si el tenant está activo en la plataforma.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Plan asignado al tenant (definido por el SuperAdmin).</summary>
    public string Plan { get; set; } = string.Empty;

    /// <summary>Parámetros de comportamiento configurables por el TenantAdmin.</summary>
    public TenantConfiguration Configuration { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
