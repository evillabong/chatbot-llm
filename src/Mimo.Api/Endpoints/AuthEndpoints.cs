using Mimo.Api.Middleware;
using Mimo.Core.DTOs.Auth;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Endpoints de autenticación de funcionarios. No requieren token previo.
/// El tenant se resuelve mediante TenantResolutionMiddleware (header X-Tenant-Slug
/// o subdominio), ya que el cliente aún no posee un JWT con el claim tenant_slug.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("AgentLogin")
            .WithSummary("Autentica a un funcionario y emite un access token + refresh token.")
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("AgentRefresh")
            .WithSummary("Renueva el access token usando un refresh token (lo rota).")
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("AgentLogout")
            .WithSummary("Revoca un refresh token (cierre de sesión).")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAgentRepository repo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokens,
        HttpContext context,
        CancellationToken ct = default)
    {
        var agent = await repo.GetByEmailAsync(request.Email, ct);

        if (agent is null || !agent.IsActive || !passwordHasher.Verify(request.Password, agent.PasswordHash))
            return Results.Unauthorized();

        // El middleware ya garantizó la resolución del tenant para esta ruta (no bypass).
        var tenantSlug = context.GetTenantSlug();
        var refresh = await refreshTokens.IssueAsync(agent.Id, ct);

        return Results.Ok(BuildResponse(agent, tenantSlug, jwtTokenService, refresh.Token, refresh.ExpiresAt));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        IAgentRepository repo,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokens,
        HttpContext context,
        CancellationToken ct = default)
    {
        var rotation = await refreshTokens.ValidateAndRotateAsync(request.RefreshToken, ct);
        if (rotation is null)
            return Results.Unauthorized();

        var agent = await repo.GetByIdAsync(rotation.AgentId, ct);
        if (agent is null || !agent.IsActive)
            return Results.Unauthorized();

        var tenantSlug = context.GetTenantSlug();
        return Results.Ok(BuildResponse(agent, tenantSlug, jwtTokenService, rotation.Token, rotation.ExpiresAt));
    }

    private static async Task<IResult> LogoutAsync(
        RefreshTokenRequest request,
        IRefreshTokenService refreshTokens,
        CancellationToken ct = default)
    {
        await refreshTokens.RevokeAsync(request.RefreshToken, ct);
        return Results.NoContent();
    }

    private static LoginResponse BuildResponse(
        Core.Models.Agent agent, string tenantSlug, IJwtTokenService jwt, string refreshToken, DateTime refreshExpiresAt)
    {
        var roleNames = agent.AgentRoles
            .Select(ar => ar.Role?.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();
        var roleIds = agent.AgentRoles.Select(ar => ar.RoleId).ToList();

        var token = jwt.GenerateAgentToken(agent, tenantSlug, roleNames, roleIds);

        return new LoginResponse(
            token.Token, token.ExpiresAtUtc, agent.Id, agent.Email, agent.FullName, roleNames,
            refreshToken, refreshExpiresAt);
    }
}
