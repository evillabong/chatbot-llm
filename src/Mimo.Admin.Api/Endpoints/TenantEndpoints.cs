using Mimo.Core.Authorization;
using Mimo.Core.Common;
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
            .RequireAuthorization(MimoAuthorization.Policies.SuperAdmin);

        // GET /tenants
        group.MapGet("/", ListTenantsAsync)
            .WithName("ListTenants")
            .WithSummary("Lista todos los tenants con paginación opcional.");

        // GET /tenants/detail?id=
        group.MapGet("/detail", GetTenantAsync)
            .WithName("GetTenant")
            .WithSummary("Obtiene un tenant por su ID (query: id).");

        // POST /tenants
        group.MapPost("/", CreateTenantAsync)
            .WithName("CreateTenant")
            .WithSummary("Registra un nuevo tenant y aprovisiona su esquema en la BD.");

        // PUT /tenants?id=
        group.MapPut("/", UpdateTenantAsync)
            .WithName("UpdateTenant")
            .WithSummary("Actualiza nombre, plan y estado activo de un tenant (query: id).");

        // DELETE /tenants?id=  (desactivación lógica, no borrado físico)
        group.MapDelete("/", DeactivateTenantAsync)
            .WithName("DeactivateTenant")
            .WithSummary("Desactiva un tenant / borrado lógico (query: id).");

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
        var pagedTenants = await repo.ListAsync(new PaginationRequest(page, pageSize), isActive, ct);

        // Conserva los metadatos de paginación y proyecta las entidades a DTOs.
        return Results.Ok(pagedTenants.Map(ToResponse));
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
        IPlanRepository planRepo,
        ITenantProvisioningService provisioning,
        IPasswordHasher passwordHasher,
        CancellationToken ct = default)
    {
        // Validar unicidad del slug
        if (await repo.SlugExistsAsync(request.Slug, ct))
            return Results.Conflict(new { error = $"El slug '{request.Slug}' ya está en uso." });

        // Validar que el plan exista en el catálogo (código canónico en minúsculas).
        var planCode = Plan.NormalizeCode(request.Plan);
        if (!await planRepo.ExistsAsync(planCode, ct))
            return Results.BadRequest(new { error = $"El plan '{request.Plan}' no existe o está inactivo." });

        var tenant = new Tenant
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name,
            Slug      = request.Slug,
            Plan      = planCode,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };

        var adminSeed = new TenantAdminSeed(
            request.AdminEmail,
            passwordHasher.Hash(request.AdminPassword),
            request.AdminFullName,
            request.AdminAlias);

        // Atomicidad: se aprovisiona el esquema PRIMERO y el registro global se persiste
        // sólo si el aprovisionamiento tuvo éxito, para no dejar tenants huérfanos (registro
        // sin esquema). Ante cualquier fallo, se elimina el esquema parcial creado.
        try
        {
            await provisioning.ProvisionAsync(tenant, adminSeed, ct);
            await repo.AddAsync(tenant, ct);
        }
        catch
        {
            // Compensación: borrar el esquema parcial. Se ignoran errores aquí para no
            // enmascarar la excepción original (el servicio registra el intento de borrado).
            try { await provisioning.DeprovisionAsync(tenant.Slug, ct); } catch { /* preservar excepción original */ }
            throw;
        }

        return Results.Created($"/tenants/detail?id={tenant.Id}", ToResponse(tenant));
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
