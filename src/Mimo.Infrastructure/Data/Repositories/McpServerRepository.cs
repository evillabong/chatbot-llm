using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio del catálogo de servidores MCP externos (#23) dentro del esquema de un tenant.
/// </summary>
public class McpServerRepository(TenantDbContext db) : IMcpServerRepository
{
    public Task<McpServer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.McpServers.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<McpServer>> ListAsync(CancellationToken ct = default)
        => await db.McpServers.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        => db.McpServers.AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId), ct);

    public async Task<McpServer> AddAsync(McpServer server, CancellationToken ct = default)
    {
        db.McpServers.Add(server);
        await db.SaveChangesAsync(ct);
        return server;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task RemoveAsync(McpServer server, CancellationToken ct = default)
    {
        db.McpServers.Remove(server);
        await db.SaveChangesAsync(ct);
    }
}
