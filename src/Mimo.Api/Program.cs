using Mimo.Api.Endpoints;
using Mimo.Api.Hubs;
using Mimo.Api.Middleware;
using Mimo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Infraestructura ──────────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Autenticación JWT ────────────────────────────────────────────────────────
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

// ── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Mimo.Infrastructure.Data.MimoDbContext>("postgres");

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// ── Middleware ───────────────────────────────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

// ── Health check ─────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ── REST Endpoints ────────────────────────────────────────────────────────────
app.MapAgentEndpoints();
app.MapRoleEndpoints();
app.MapDocumentEndpoints();
app.MapConversationEndpoints();

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
