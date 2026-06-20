using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Auth;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Admin.Api.Endpoints;

/// <summary>
/// Gestión autenticada de super administradores (#10). Solo un SuperAdmin puede crear/listar/desactivar
/// otros. El bootstrap del primero sigue en `/auth/setup` (anónimo, solo si no existe ninguno).
/// </summary>
public static class SuperAdminEndpoints
{
    public static IEndpointRouteBuilder MapSuperAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/superadmins")
            .WithTags("SuperAdmins")
            .RequireAuthorization(MimoAuthorization.Policies.SuperAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListSuperAdmins")
            .WithSummary("Lista los super administradores (sin credenciales).")
            .Produces<List<SuperAdminResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateSuperAdmin")
            .WithSummary("Crea un super administrador adicional.")
            .Produces<SuperAdminResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/deactivate", DeactivateAsync)
            .WithName("DeactivateSuperAdmin")
            .WithSummary("Desactiva un super administrador (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(ISuperAdminRepository repo, CancellationToken ct = default)
    {
        var items = await repo.ListAsync(ct);
        return Results.Ok(items.Select(ToResponse).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateSuperAdminRequest request,
        ISuperAdminRepository repo,
        IPasswordHasher passwordHasher,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        if (await repo.EmailExistsAsync(email, ct))
            return Results.Conflict(new { error = "Ya existe un super administrador con ese correo." });

        var superAdmin = new SuperAdmin
        {
            Id           = Guid.NewGuid(),
            Email        = email,
            FullName     = request.FullName,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        await repo.AddAsync(superAdmin, ct);

        return Results.Created("/superadmins", ToResponse(superAdmin));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid id,
        ISuperAdminRepository repo,
        HttpContext context,
        CancellationToken ct = default)
    {
        // No permitir auto-desactivarse (evita perder la propia sesión por error).
        var callerId = context.User.FindFirst("super_admin_id")?.Value;
        if (Guid.TryParse(callerId, out var caller) && caller == id)
            return Results.BadRequest(new { error = "No puedes desactivar tu propia cuenta." });

        var target = await repo.GetByIdAsync(id, ct);
        if (target is null)
            return Results.NotFound(new { error = "Super administrador no encontrado." });

        if (!target.IsActive)
            return Results.NoContent(); // ya inactivo: idempotente

        // No dejar la plataforma sin ningún super administrador activo.
        if (await repo.CountActiveAsync(ct) <= 1)
            return Results.BadRequest(new { error = "No puedes desactivar al último super administrador activo." });

        target.IsActive = false;
        await repo.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static SuperAdminResponse ToResponse(SuperAdmin s) =>
        new(s.Id, s.Email, s.FullName, s.IsActive, s.CreatedAt);
}
