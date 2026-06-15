using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.MultiTenancy;

/// <summary>
/// Implementación scoped de <see cref="ITenantSchemaProvider"/>: un simple portador
/// del esquema resuelto durante la petición.
/// </summary>
public class TenantSchemaProvider : ITenantSchemaProvider
{
    public string? Schema { get; set; }
}
