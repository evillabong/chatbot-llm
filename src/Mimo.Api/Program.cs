using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Mimo.Api.Channels;
using Mimo.Api.Endpoints;
using Mimo.Api.Hubs;
using Mimo.Api.Middleware;
using Mimo.Api.Services;
using Mimo.Api.Workers;
using Mimo.Core.Authorization;
using Mimo.Core.Interfaces;
using Mimo.Infrastructure;
using Mimo.Infrastructure.Data;
using Mimo.Infrastructure.Data.Seeding;

var builder = WebApplication.CreateBuilder(args);

// ── Infraestructura ──────────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Data Protection (anillo de llaves compartido entre ambas APIs) ─────────────
// Mimo.Admin.Api cifra las API keys de los conectores; Mimo.Api las descifra. Para
// que funcione entre procesos, ambas comparten ApplicationName y ubicación de llaves.
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MIMO", "dp-keys");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("MIMO")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

// ── Notificaciones en tiempo real ────────────────────────────────────────────
// INotificationService se implementa en esta capa (Mimo.Api) porque usa SignalR
// de ASP.NET Core, que no debe referenciar desde Mimo.Infrastructure.
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// ── Conector WebChat (keyed DI) ───────────────────────────────────────────────
// WebChatConnector vive en Mimo.Api porque requiere IHubContext<ChatHub>.
// Los conectores de canales externos (Facebook, WhatsApp, Telegram, Instagram)
// se registran en Mimo.Infrastructure/DependencyInjection.cs.
builder.Services.AddKeyedScoped<IChannelConnector, WebChatConnector>("webchat");

// ── Autenticación JWT ────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey     = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado.");
var jwtIssuer  = jwtSection["Issuer"] ?? "mimo-api";
var jwtAudience = jwtSection["Audience"] ?? "mimo-clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Conservar los nombres de claim tal cual se emiten ("role", "agent_id", "tenant_slug").
        // Sin esto, JwtBearer remapea "role" a ClaimTypes.Role y las políticas RequireClaim fallan (403).
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

        // Permitir que SignalR envíe el token como query param para WebSockets
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path        = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

// ── Políticas de autorización por rol ─────────────────────────────────────────
// TenantAdmin: gestión de funcionarios, roles y administración del conocimiento.
// Agent:       operaciones de atención (tickets, chat interno, lectura de conocimiento).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(MimoAuthorization.Policies.TenantAdmin, policy =>
        policy.RequireClaim("role", MimoAuthorization.Roles.Administrator));

    options.AddPolicy(MimoAuthorization.Policies.Agent, policy =>
        policy.RequireClaim("agent_id"));
});

// ── CORS ───────────────────────────────────────────────────────────────────────
// En Development (dotnet run o publicación local en IIS con ASPNETCORE_ENVIRONMENT=Development)
// se permite cualquier origen/encabezado/método. En otros entornos se restringe a los orígenes
// declarados en "Cors:AllowedOrigins". Autenticación por Bearer (no cookies).
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

// ── SignalR ──────────────────────────────────────────────────────────────────
// El filtro fija el search_path del tenant en cada invocación de hub (los hubs no pasan
// por TenantResolutionMiddleware salvo en el handshake; ver TenantHubFilter).
builder.Services.AddSignalR(options => options.AddFilter<Mimo.Api.Hubs.TenantHubFilter>());

// ── Workers de background ─────────────────────────────────────────────────────
builder.Services.AddHostedService<InactivityTimeoutWorker>();
builder.Services.AddHostedService<QueueNotificationWorker>();

// ── OpenAPI ──────────────────────────────────────────────────────────────────
// OpenAPI 3.0 (no 3.1): mejor compatibilidad con generadores de cliente como Kiota.
builder.Services.AddOpenApi(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);

// ── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GlobalDbContext>("postgres-global")
    .AddDbContextCheck<TenantDbContext>("postgres-tenant");

var app = builder.Build();

// ── Migrar esquema global al arrancar ────────────────────────────────────────
// Asegura que la tabla de tenants exista en el esquema public antes de aceptar tráfico.
await using (var scope = app.Services.CreateAsyncScope())
{
    var globalDb = scope.ServiceProvider.GetRequiredService<GlobalDbContext>();
    await globalDb.Database.MigrateAsync();

    // Sembrar el catálogo de planes por defecto (requerido por el aprovisionamiento de tenants).
    await PlanSeeder.SeedDefaultAsync(globalDb);

    // Migrar la configuración de IA de appsettings a la BD si aún no existe ningún conector.
    var secretProtector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
    await AiConnectorSeeder.SeedDefaultAsync(globalDb, app.Configuration, secretProtector);

    // Datos de desarrollo (funcionarios/roles/conocimiento de demo, credenciales conocidas).
    if (app.Environment.IsDevelopment())
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();

        var tenantSeeder = new TenantDevDataSeeder(
            contextFactory, globalDb, passwordHasher,
            scope.ServiceProvider.GetRequiredService<ILogger<TenantDevDataSeeder>>());
        await tenantSeeder.SeedAsync();

        // Negocio de demostración coherente (AndinaShop): admin, funcionarios, clientes,
        // conversaciones, tickets, encuestas y conocimiento.
        var businessSeeder = new BusinessDemoSeeder(
            globalDb,
            scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>(),
            contextFactory, passwordHasher,
            scope.ServiceProvider.GetRequiredService<ILogger<BusinessDemoSeeder>>());
        await businessSeeder.SeedAsync();
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// ── Middleware ───────────────────────────────────────────────────────────────
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

// ── Health check ─────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ── REST Endpoints ────────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapAgentEndpoints();
app.MapRoleEndpoints();
app.MapDocumentEndpoints();
app.MapTenantConfigurationEndpoints();
app.MapConversationEndpoints();
app.MapTicketEndpoints();
app.MapInternalChatEndpoints();
app.MapSurveyEndpoints();
app.MapWebhookEndpoints();

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
app.MapHub<ChatHub>("/hubs/chat");       // ciudadanos
app.MapHub<TicketHub>("/hubs/tickets"); // funcionarios

app.Run();
