using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mimo.Admin.Api.Endpoints;
using Mimo.Core.Authorization;
using Mimo.Core.Interfaces;
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

// ── Data Protection (anillo de llaves compartido con Mimo.Api) ─────────────────
// Este API cifra las API keys de los conectores; Mimo.Api las descifra. Mismo
// ApplicationName y ubicación de llaves para que el cifrado sea interoperable.
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MIMO", "dp-keys");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("MIMO")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

// ── Autenticación JWT ────────────────────────────────────────────────────────
var jwtSection  = builder.Configuration.GetSection("Jwt");
var jwtKey      = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado.");
var jwtIssuer   = jwtSection["Issuer"]   ?? "mimo-admin-api";
var jwtAudience = jwtSection["Audience"] ?? "mimo-admin-clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Conservar los nombres de claim tal cual se emiten (en particular "role").
        // Sin esto, JwtBearer remapea "role" a ClaimTypes.Role y la política SuperAdmin falla (403).
        options.MapInboundClaims = false;

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

// ── CORS ───────────────────────────────────────────────────────────────────────
// En Development (dotnet run o publicación local en IIS con ASPNETCORE_ENVIRONMENT=Development)
// se permite cualquier origen/encabezado/método. En otros entornos se restringe a los orígenes
// declarados en "Cors:AllowedOrigins". Mimo.Admin.App (WASM) consume esta API desde otro origen.
// Autenticación por Bearer (no cookies).
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (isDevelopment)
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }
    else
    {
        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}));

// ── OpenAPI ──────────────────────────────────────────────────────────────────
// OpenAPI 3.0 (no 3.1): mejor compatibilidad con generadores de cliente como Kiota.
builder.Services.AddOpenApi(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);

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

    // Datos de desarrollo: SuperAdmin con credenciales conocidas + políticas de IA por plan.
    if (app.Environment.IsDevelopment())
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await SuperAdminSeeder.SeedDevAsync(globalDb, passwordHasher);
        await AiPlanPolicySeeder.SeedDevAsync(globalDb);
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapSuperAdminEndpoints();
app.MapTenantEndpoints();
app.MapPlanEndpoints();
app.MapAiConnectorEndpoints();
app.MapAiPlanPolicyEndpoints();
app.MapAiUsageEndpoints();

app.Run();
