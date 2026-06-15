using Mimo.Core.Models.Ai;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Consulta agregada del uso de IA (esquema public, GlobalDbContext) para reportes del SuperAdmin.
/// </summary>
public interface IAiUsageRepository
{
    /// <summary>
    /// Resume el uso por tenant en el rango [from, to). Si se indica tenantId, lo limita a ese tenant.
    /// </summary>
    Task<IReadOnlyList<AiUsageSummary>> GetSummaryAsync(
        DateTime fromUtc, DateTime toUtc, Guid? tenantId, CancellationToken ct = default);
}
