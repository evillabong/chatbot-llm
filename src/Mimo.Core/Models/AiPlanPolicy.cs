namespace Mimo.Core.Models;

/// <summary>
/// Política de uso de IA asociada a un plan (esquema public, gestionada por el SuperAdmin).
///
/// Determina qué proveedores/modelos puede usar un tenant según su plan y la cuota de
/// consumo mensual. Si un plan no tiene política, el comportamiento es permisivo
/// (conector activo, sin cuota) para no romper tenants existentes.
/// </summary>
public class AiPlanPolicy
{
    public Guid Id { get; set; }

    /// <summary>Código de plan al que aplica (coincide con Tenant.Plan). Único.</summary>
    public string PlanCode { get; set; } = string.Empty;

    /// <summary>
    /// Claves de proveedores permitidos (ver AiProviders). Si está vacía, el plan no
    /// restringe por proveedor (cualquier conector activo es válido).
    /// </summary>
    public List<string> AllowedProviders { get; set; } = [];

    /// <summary>Cuota mensual de solicitudes de IA. 0 = ilimitado.</summary>
    public int MonthlyRequestQuota { get; set; }

    /// <summary>Cuota mensual de tokens consumidos. 0 = ilimitado.</summary>
    public long MonthlyTokenQuota { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
