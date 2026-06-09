using Mimo.Admin.Api.Endpoints;
using Mimo.Infrastructure;
using Mimo.Infrastructure.Data;

/// <summary>
/// API exclusiva de administración de la plataforma MIMO.
/// Opera únicamente sobre el esquema public (catálogo global de tenants).
/// No debe estar expuesta en el mismo host que Mimo.Api.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// ── Infraestructura (esquema public + aprovisionamiento de tenants) ───────────
builder.Services.AddAdminInfrastructure(builder.Configuration);

// ── Autenticación JWT ────────────────────────────────────────────────────────
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization(options =>
{
    // Solo SuperAdmin puede acceder a esta API
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireClaim("role", "SuperAdmin"));
});

// ── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GlobalDbContext>("postgres-global");

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapTenantEndpoints();

app.Run();
