using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Mcp;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Catálogo de servidores MCP externos por organización (#23). Gestión para TenantAdmin: declarar
/// endpoint, allowlist de herramientas (deny-by-default), token de autenticación cifrado (write-only,
/// ADR 0010) y habilitación. La invocación mediada por el gateway es un corte posterior.
/// </summary>
public static class McpServerEndpoints
{
    public static IEndpointRouteBuilder MapMcpServerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/mcp-servers")
            .WithTags("McpServers")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListMcpServers")
            .WithSummary("Lista los servidores MCP externos del tenant.")
            .Produces<List<McpServerResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateMcpServer")
            .WithSummary("Registra un servidor MCP externo.")
            .Produces<McpServerResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateMcpServer")
            .WithSummary("Actualiza un servidor MCP externo (query: id).")
            .Produces<McpServerResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/", DeleteAsync)
            .WithName("DeleteMcpServer")
            .WithSummary("Elimina un servidor MCP externo (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(IMcpServerRepository repo, CancellationToken ct = default)
    {
        var items = await repo.ListAsync(ct);
        return Results.Ok(items.Select(ToResponse).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateMcpServerRequest request,
        IMcpServerRepository repo,
        ISecretProtector secretProtector,
        HttpContext context,
        CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (!IsValidEndpoint(request.Endpoint))
            return Results.BadRequest(new { error = "El endpoint debe ser una URL absoluta http/https." });
        if (await repo.NameExistsAsync(name, null, ct))
            return Results.Conflict(new { error = "Ya existe un servidor MCP con ese nombre." });

        var server = new McpServer
        {
            Id             = Guid.NewGuid(),
            TenantId       = context.GetTenantId(),
            Name           = name,
            Endpoint       = request.Endpoint.Trim(),
            AuthToken      = string.IsNullOrWhiteSpace(request.AuthToken) ? null : secretProtector.Protect(request.AuthToken),
            AllowedTools   = NormalizeTools(request.AllowedTools),
            IsEnabled      = request.IsEnabled,
            TimeoutSeconds = NormalizeTimeout(request.TimeoutSeconds),
            CreatedAt      = DateTime.UtcNow
        };
        await repo.AddAsync(server, ct);

        return Results.Created("/mcp-servers", ToResponse(server));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateMcpServerRequest request,
        IMcpServerRepository repo,
        ISecretProtector secretProtector,
        CancellationToken ct = default)
    {
        var server = await repo.GetByIdAsync(id, ct);
        if (server is null)
            return Results.NotFound(new { error = "Servidor MCP no encontrado." });

        var name = request.Name.Trim();
        if (!IsValidEndpoint(request.Endpoint))
            return Results.BadRequest(new { error = "El endpoint debe ser una URL absoluta http/https." });
        if (await repo.NameExistsAsync(name, id, ct))
            return Results.Conflict(new { error = "Ya existe un servidor MCP con ese nombre." });

        server.Name           = name;
        server.Endpoint       = request.Endpoint.Trim();
        // Write-only: token en blanco conserva el existente; uno nuevo lo reemplaza.
        if (!string.IsNullOrWhiteSpace(request.AuthToken))
            server.AuthToken = secretProtector.Protect(request.AuthToken);
        server.AllowedTools   = NormalizeTools(request.AllowedTools);
        server.IsEnabled      = request.IsEnabled;
        server.TimeoutSeconds = NormalizeTimeout(request.TimeoutSeconds);
        server.UpdatedAt      = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(server));
    }

    private static async Task<IResult> DeleteAsync(Guid id, IMcpServerRepository repo, CancellationToken ct = default)
    {
        var server = await repo.GetByIdAsync(id, ct);
        if (server is null)
            return Results.NotFound(new { error = "Servidor MCP no encontrado." });

        await repo.RemoveAsync(server, ct);
        return Results.NoContent();
    }

    private static bool IsValidEndpoint(string? endpoint)
        => Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string[] NormalizeTools(string[]? tools)
        => tools is null
            ? []
            : tools.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).ToArray();

    private static int NormalizeTimeout(int seconds) => Math.Clamp(seconds, 1, 120);

    private static McpServerResponse ToResponse(McpServer s) => new(
        s.Id, s.Name, s.Endpoint, s.AllowedTools, s.IsEnabled, s.TimeoutSeconds,
        !string.IsNullOrEmpty(s.AuthToken), s.CreatedAt, s.UpdatedAt);
}
