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
            .WithSummary("Autentica a un funcionario y emite un token JWT.")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAgentRepository repo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        HttpContext context,
        CancellationToken ct = default)
    {
        var agent = await repo.GetByEmailAsync(request.Email, ct);

        if (agent is null || !agent.IsActive || !passwordHasher.Verify(request.Password, agent.PasswordHash))
            return Results.Unauthorized();

        // El middleware ya garantizó la resolución del tenant para esta ruta (no bypass).
        var tenantSlug = context.GetTenantSlug();

        var roleNames = agent.AgentRoles
            .Select(ar => ar.Role?.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();

        var token = jwtTokenService.GenerateAgentToken(agent, tenantSlug, roleNames);

        return Results.Ok(new LoginResponse(
            token.Token, token.ExpiresAtUtc, agent.Id, agent.Email, agent.FullName, roleNames));
    }
}
