using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a los conectores de IA (esquema public, GlobalDbContext). Gestionado por el SuperAdmin.
/// </summary>
public interface IAiConnectorRepository
{
    Task<IReadOnlyList<AiConnector>> ListAsync(CancellationToken ct = default);
    Task<AiConnector?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ProviderExistsAsync(string provider, CancellationToken ct = default);
    Task<AiConnector> AddAsync(AiConnector connector, CancellationToken ct = default);
    Task UpdateAsync(AiConnector connector, CancellationToken ct = default);

    /// <summary>
    /// Marca un conector como el activo de la plataforma y desactiva el resto, de forma atómica.
    /// Devuelve false si el conector no existe.
    /// </summary>
    Task<bool> SetActiveAsync(Guid id, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
