using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Catálogo de servidores MCP externos (#23) por encima del cliente Kiota. El token es write-only:
/// no se devuelve y, al actualizar en blanco, se conserva.
/// </summary>
public sealed class McpServersService(MimoApiClient api)
{
    public async Task<List<McpServerResponse>> ListAsync(CancellationToken ct = default)
    {
        var result = await api.McpServers.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<McpServerResponse?> CreateAsync(CreateMcpServerRequest request, CancellationToken ct = default) =>
        api.McpServers.PostAsync(request, cancellationToken: ct);

    public Task<McpServerResponse?> UpdateAsync(Guid id, UpdateMcpServerRequest request, CancellationToken ct = default) =>
        api.McpServers.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        api.McpServers.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
