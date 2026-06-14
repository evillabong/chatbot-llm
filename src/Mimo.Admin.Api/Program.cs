using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mimo.Admin.Api.Endpoints;
using Mimo.Core.Authorization;
using Mimo.Infrastructure;
using Mimo.Infrastructure.Data;
using Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// API exclusiva de administración de la plataforma MIMO.
/// Opera únicamente sobre el esquema public (catálogo global de tenants).
/// No debe estar expuesta en el mismo host que Mimo.Api.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// ── Infraestructura (esquema public + aprovisionamiento de tenants) ───────────
builder.Services.AddAdminInfrastructure(builder.Configuration);

// ── Autenticación JWT ────────────────────────────────────────────────────────
var jwtSection  = builder.Configuration.GetSection("Jwt");
var jwtKey      = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado.");
var jwtIssuer   = jwtSection["Issuer"]   ?? "mimo-admin-api";
var jwtAudience = jwtSection["Audience"] ?? "mimo-admin-clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Solo SuperAdmin puede acceder a esta API
    options.AddPolicy(MimoAuthorization.Policies.SuperAdmin, policy =>
        policy.RequireClaim("role", MimoAuthorization.Roles.SuperAdmin));
});

// ── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GlobalDbContext>("postgres-global");

var app = builder.Build();

// ── Migrar esquema global al arrancar ────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var globalDb = scope.ServiceProvider.GetRequiredService<GlobalDbContext>();
    await globalDb.Database.MigrateAsync();

    // Catálogo de planes por defecto: necesario para crear tenants desde este API.
    await PlanSeeder.SeedDefaultAsync(globalDb);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapTenantEndpoints();

app.Run();
