using Microsoft.EntityFrameworkCore;
using Mimo.Api.Middleware;
using Mimo.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<MimoDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsql => npgsql.UseVector()));

// ── Autenticación JWT ────────────────────────────────────────────────────────
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

// ── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MimoDbContext>("postgres");

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

// ── Health check ─────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ── Endpoints (se registrarán en archivos separados) ─────────────────────────
// app.MapWebhookEndpoints();
// app.MapTicketEndpoints();
// app.MapDocumentEndpoints();

app.Run();
