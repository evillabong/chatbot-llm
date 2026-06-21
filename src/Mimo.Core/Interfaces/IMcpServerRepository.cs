using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso al catálogo de servidores MCP externos (#23) dentro del esquema de un tenant.
/// </summary>
public interface IMcpServerRepository
{
    Task<McpServer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<McpServer>> ListAsync(CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
    Task<McpServer> AddAsync(McpServer server, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task RemoveAsync(McpServer server, CancellationToken ct = default);
}
