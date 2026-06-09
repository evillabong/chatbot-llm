using Microsoft.EntityFrameworkCore;
using Mimo.Infrastructure.Data;

/// <summary>
/// API exclusiva de administración de la plataforma MIMO.
/// Opera únicamente sobre el esquema public (catálogo global de tenants).
/// No debe estar expuesta en el mismo host que Mimo.Api.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// ── Base de datos (esquema public únicamente) ────────────────────────────────
builder.Services.AddDbContext<MimoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Autenticación JWT ────────────────────────────────────────────────────────
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization(options =>
{
    // Solo SuperAdmin puede acceder a esta API
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireClaim("role", "SuperAdmin"));
});

// ── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MimoDbContext>("postgres");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ── Endpoints (se registrarán en archivos separados) ─────────────────────────
// app.MapTenantEndpoints();
// app.MapPlanEndpoints();
// app.MapMonitoringEndpoints();

app.Run();
