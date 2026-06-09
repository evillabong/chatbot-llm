using Mimo.Core.DTOs.Auth;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Admin.Api.Endpoints;

/// <summary>
/// Endpoints de autenticación y aprovisionamiento del primer super administrador.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("SuperAdminLogin")
            .WithSummary("Autentica a un super administrador y emite un token JWT.")
            .AllowAnonymous();

        group.MapPost("/setup", SetupAsync)
            .WithName("SetupSuperAdmin")
            .WithSummary("Crea el primer super administrador de la plataforma. Solo funciona si aún no existe ninguno.")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ISuperAdminRepository repo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        CancellationToken ct = default)
    {
        var superAdmin = await repo.GetByEmailAsync(request.Email, ct);

        if (superAdmin is null || !superAdmin.IsActive || !passwordHasher.Verify(request.Password, superAdmin.PasswordHash))
            return Results.Unauthorized();

        var token = jwtTokenService.GenerateSuperAdminToken(superAdmin);

        return Results.Ok(new LoginResponse(
            token.Token, token.ExpiresAtUtc, superAdmin.Id, superAdmin.Email, superAdmin.FullName, ["SuperAdmin"]));
    }

    private static async Task<IResult> SetupAsync(
        SetupSuperAdminRequest request,
        ISuperAdminRepository repo,
        IPasswordHasher passwordHasher,
        CancellationToken ct = default)
    {
        // Bootstrap: solo se permite crear el primer SuperAdmin. Una vez exista alguno,
        // la creación de nuevos super administradores debe hacerse desde un endpoint
        // autenticado (fuera del alcance actual) o directamente en la base de datos.
        if (await repo.AnyAsync(ct))
            return Results.Conflict(new { error = "Ya existe al menos un super administrador. Use /auth/login." });

        var superAdmin = new SuperAdmin
        {
            Id           = Guid.NewGuid(),
            Email        = request.Email,
            FullName     = request.FullName,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        await repo.AddAsync(superAdmin, ct);

        return Results.Created("/auth/setup", new { superAdmin.Id, superAdmin.Email });
    }
}
