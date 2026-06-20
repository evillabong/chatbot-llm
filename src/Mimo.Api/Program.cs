using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Mimo.Api.Authentication;
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
    })
    // Esquema por API key para la superficie de interoperabilidad (cabecera X-Api-Key, ADR 0015).
    // No es el esquema por defecto: solo lo activan los endpoints con la política Integration.
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });

// ── Políticas de autorización por rol ─────────────────────────────────────────
// TenantAdmin: gestión de funcionarios, roles y administración del conocimiento.
// Agent:       operaciones de atención (tickets, chat interno, lectura de conocimiento).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(MimoAuthorization.Policies.TenantAdmin, policy =>
        policy.RequireClaim("role", MimoAuthorization.Roles.Administrator));

    options.AddPolicy(MimoAuthorization.Policies.Agent, policy =>
        policy.RequireClaim("agent_id"));

    // Interoperabilidad: autentica exclusivamente por el esquema ApiKey (no JWT) y exige que el
    // principal provenga de una API key válida. El tenant ya quedó resuelto por la propia clave.
    options.AddPolicy(MimoAuthorization.Policies.Integration, policy =>
        policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
              .RequireClaim(ApiKeyAuthenticationHandler.Claims.AuthMethod, ApiKeyAuthenticationHandler.Claims.ApiKeyValue));
});

// ── CORS ───────────────────────────────────────────────────────────────────────
// En Development (dotnet run o publicación local en IIS con ASPNETCORE_ENVIRONMENT=Development)
// se permite cualquier origen/encabezado/método. En otros entornos se restringe a los orígenes
// declarados en "Cors:AllowedOrigins". Autenticación por Bearer (no cookies).
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (isDevelopment)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            // Producción: orígenes explícitos (la consola/app suele servirse desde un sitio distinto
            // al de la API). AllowCredentials habilita los hubs SignalR autenticados cross-origin
            // (TicketHub): los WebSockets/negotiate cross-origin con credenciales NO admiten
            // AllowAnyOrigin (pendings #5). Configurar "Cors:AllowedOrigins" con el origen de mimo.app.
            var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });

    // Superficie pública del WebChat embebible (Fase E): el widget se incrusta en sitios de
    // terceros (dominios arbitrarios), así que esta superficie permite CUALQUIER origen. Es segura
    // porque es anónima, sin cookies y el tenant se resuelve por X-Tenant-Slug (slug público).
    options.AddPolicy(WebChatEndpoints.CorsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ── IP real tras proxy inverso (pendings #34) ──────────────────────────────────────────────────
// El rate limiting y los logs dependen de la IP del cliente. Tras un proxy (IIS out-of-process,
// nginx, balanceador) la IP real llega en X-Forwarded-For. Por defecto solo se confía en loopback
// (proxy co-localizado); para balanceadores externos, declarar sus IPs en "ForwardedHeaders:KnownProxies".
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var ip in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        if (IPAddress.TryParse(ip, out var addr)) options.KnownProxies.Add(addr);
});

// ── Rate limiting de la superficie pública del WebChat (anti-abuso, pendings #32) ───────────────
// La superficie del widget es anónima y abierta a cualquier origen; sin límites, un tercero podría
// abrir conversaciones masivas o inundar el bot (cada mensaje cuesta LLM). Se limita POR IP del
// cliente. Nota: tras un proxy inverso (IIS/ANCM) la IP real viene en X-Forwarded-For; usar
// UseForwardedHeaders en producción para que RemoteIpAddress sea la real (ver docs/pendings).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        if (ctx.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Demasiadas solicitudes. Intenta de nuevo en unos momentos." }, ct);
    };

    // Crear conversación / encuesta / handshake del hub: estricto (lo más caro de abusar).
    options.AddPolicy(WebChatEndpoints.RateLimitStart, ClientIpFixedWindow(permitLimit: 10, windowMinutes: 1));
    // Envío de mensajes (REST): más holgado pero acotado (cuesta LLM).
    options.AddPolicy(WebChatEndpoints.RateLimitChat, ClientIpFixedWindow(permitLimit: 30, windowMinutes: 1));
});

// Limitador de ventana fija particionado por IP del cliente (host:puerto se ignora, solo IP).
static Func<HttpContext, RateLimitPartition<string>> ClientIpFixedWindow(int permitLimit, int windowMinutes) =>
    httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window      = TimeSpan.FromMinutes(windowMinutes)
            });
    };

// ── SignalR ──────────────────────────────────────────────────────────────────
// El filtro fija el search_path del tenant en cada invocación de hub (los hubs no pasan
// por TenantResolutionMiddleware salvo en el handshake; ver TenantHubFilter).
builder.Services.AddSignalR(options => options.AddFilter<Mimo.Api.Hubs.TenantHubFilter>());

// ── Workers de background ─────────────────────────────────────────────────────
// Solo el de notificación de cola vive aquí: empuja por SignalR (IHubContext<TicketHub>), que está
// atado a este proceso. Los workers de BD/HTTP (entrega de webhooks, cierre por inactividad) se
// alojan en Mimo.Worker, un host separado que se despliega y escala aparte (ADR 0017).
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

    // Migrar los esquemas de los tenants YA existentes. El aprovisionamiento migra cada tenant al
    // crearlo, pero una migración de TenantDbContext nueva (p. ej. AddWebhooks) no llega sola a los
    // tenants creados antes. Idempotente: MigrateAsync no hace nada si el esquema está al día.
    await TenantSchemaMigrator.MigrateExistingTenantsAsync(scope.ServiceProvider);

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
// Primero: normaliza la IP/protocolo del cliente desde X-Forwarded-* (antes de CORS y rate limiting).
app.UseForwardedHeaders();
app.UseCors();
// Sirve el widget embebible del WebChat (wwwroot/webchat/widget.js) y la página demo (Fase E).
app.UseStaticFiles();
// Rate limiting de la superficie pública del WebChat (anti-abuso); aplica solo donde se declara la política.
app.UseRateLimiter();
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
app.MapIntegrationEndpoints();
app.MapIntegrationApiEndpoints();
app.MapWebhookSubscriptionEndpoints();
app.MapConversationEndpoints();
app.MapTicketEndpoints();
app.MapInternalChatEndpoints();
app.MapSurveyEndpoints();
app.MapWebhookEndpoints();
app.MapWebChatEndpoints();

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
// ChatHub es la superficie pública del WebChat embebible: CORS abierto (negotiate cross-origin) y
// tenant por ?tenant_slug en el handshake (el navegador no puede fijar cabeceras en WebSocket).
app.MapHub<ChatHub>("/hubs/chat")
   .RequireCors(WebChatEndpoints.CorsPolicy)
   .RequireRateLimiting(WebChatEndpoints.RateLimitStart); // ciudadanos: limita el negotiate por IP
app.MapHub<TicketHub>("/hubs/tickets"); // funcionarios

app.Run();
