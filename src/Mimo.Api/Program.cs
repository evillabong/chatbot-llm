using Mimo.Api.Endpoints;
using Mimo.Api.Hubs;
using Mimo.Api.Middleware;
using Mimo.Api.Services;
using Mimo.Core.Interfaces;
using Mimo.Infrastructure;
using Mimo.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Infraestructura ──────────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Notificaciones en tiempo real (SignalR) ───────────────────────────────────
// INotificationService se implementa con SignalR desde esta capa (Api), ya que
// la infraestructura no debe referenciar ASP.NET Core SignalR directamente.
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// ── Autenticación JWT ────────────────────────────────────────────────────────
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

// ── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GlobalDbContext>("postgres-global")
    .AddDbContextCheck<TenantDbContext>("postgres-tenant");

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
app.MapTicketEndpoints();
app.MapInternalChatEndpoints();

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
app.MapHub<ChatHub>("/hubs/chat");       // ciudadanos
app.MapHub<TicketHub>("/hubs/tickets"); // funcionarios

app.Run();
