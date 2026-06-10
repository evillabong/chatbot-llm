using Mimo.Core.DTOs.Tenant;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Admin.Api.Endpoints;

/// <summary>
/// Endpoints CRUD de tenants. Solo accesibles con política SuperAdmin.
/// Al crear un tenant se aprovisiona automáticamente su esquema en PostgreSQL
/// mediante TenantProvisioningService (EF Core migrations, sin SQL manual).
/// </summary>
public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tenants")
            .WithTags("Tenants")
            .RequireAuthorization("SuperAdmin");

        // GET /tenants
        group.MapGet("/", ListTenantsAsync)
            .WithName("ListTenants")
            .WithSummary("Lista todos los tenants con paginación opcional.");

        // GET /tenants/{id}
        group.MapGet("/{id:guid}", GetTenantAsync)
            .WithName("GetTenant")
            .WithSummary("Obtiene un tenant por su ID.");

        // POST /tenants
        group.MapPost("/", CreateTenantAsync)
            .WithName("CreateTenant")
            .WithSummary("Registra un nuevo tenant y aprovisiona su esquema en la BD.");

        // PUT /tenants/{id}
        group.MapPut("/{id:guid}", UpdateTenantAsync)
            .WithName("UpdateTenant")
            .WithSummary("Actualiza nombre, plan y estado activo de un tenant.");

        // DELETE /tenants/{id}  (desactivación lógica, no borrado físico)
        group.MapDelete("/{id:guid}", DeactivateTenantAsync)
            .WithName("DeactivateTenant")
            .WithSummary("Desactiva un tenant (borrado lógico).");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> ListTenantsAsync(
        ITenantRepository repo,
        int page = 1,
        int pageSize = 20,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var (items, total) = await repo.ListAsync(page, pageSize, isActive, ct);

        var response = new TenantListResponse(
            items.Select(ToResponse).ToList(),
            total, page, pageSize);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetTenantAsync(
        Guid id,
        ITenantRepository repo,
        CancellationToken ct = default)
    {
        var tenant = await repo.GetByIdAsync(id, ct);
        return tenant is null
            ? Results.NotFound(new { error = "Tenant no encontrado." })
            : Results.Ok(ToResponse(tenant));
    }

    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request,
        ITenantRepository repo,
        ITenantProvisioningService provisioning,
        IPasswordHasher passwordHasher,
        CancellationToken ct = default)
    {
        // Validar unicidad del slug
        if (await repo.SlugExistsAsync(request.Slug, ct))
            return Results.Conflict(new { error = $"El slug '{request.Slug}' ya está en uso." });

        var tenant = new Tenant
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name,
            Slug      = request.Slug,
            Plan      = request.Plan,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };

        // 1. Persistir el registro del tenant en el esquema public
        await repo.AddAsync(tenant, ct);

        // 2. Provisionar el esquema PostgreSQL del tenant, aplicar migraciones (EF Core) y
        //    crear el rol "Administrador" + el funcionario administrador inicial.
        var adminSeed = new TenantAdminSeed(
            request.AdminEmail,
            passwordHasher.Hash(request.AdminPassword),
            request.AdminFullName,
            request.AdminAlias);

        await provisioning.ProvisionAsync(tenant, adminSeed, ct);

        return Results.Created($"/tenants/{tenant.Id}", ToResponse(tenant));
    }

    private static async Task<IResult> UpdateTenantAsync(
        Guid id,
        UpdateTenantRequest request,
        ITenantRepository repo,
        CancellationToken ct = default)
    {
        var tenant = await repo.GetByIdAsync(id, ct);
        if (tenant is null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        tenant.Name      = request.Name;
        tenant.Plan      = request.Plan;
        tenant.IsActive  = request.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;

        await repo.UpdateAsync(tenant, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(tenant));
    }

    private static async Task<IResult> DeactivateTenantAsync(
        Guid id,
        ITenantRepository repo,
        CancellationToken ct = default)
    {
        var tenant = await repo.GetByIdAsync(id, ct);
        if (tenant is null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        if (!tenant.IsActive)
            return Results.Ok(new { message = "El tenant ya estaba inactivo." });

        tenant.IsActive  = false;
        tenant.UpdatedAt = DateTime.UtcNow;

        await repo.UpdateAsync(tenant, ct);
        await repo.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static TenantResponse ToResponse(Tenant t) =>
        new(t.Id, t.Name, t.Slug, t.Plan, t.IsActive, t.CreatedAt);
}
