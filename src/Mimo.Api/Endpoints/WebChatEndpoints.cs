using Microsoft.EntityFrameworkCore;
using Mimo.Api.Middleware;
using Mimo.Core.DTOs.WebChat;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Superficie pública del WebChat embebible (Fase E). El widget (wwwroot/webchat/widget.js) se
/// incrusta en el sitio del cliente y consume esta config + los endpoints anónimos de conversación.
/// El tenant se resuelve por X-Tenant-Slug (slug público en el snippet); CORS abierto a cualquier
/// origen vía la política <see cref="CorsPolicy"/>.
/// </summary>
public static class WebChatEndpoints
{
    /// <summary>Política CORS pública (cualquier origen) para la superficie del widget.</summary>
    public const string CorsPolicy = "webchat";

    public static IEndpointRouteBuilder MapWebChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webchat")
            .WithTags("WebChat")
            .RequireCors(CorsPolicy)
            .AllowAnonymous();

        group.MapGet("/config", GetConfigAsync)
            .WithName("WebChatConfig")
            .WithSummary("Config pública del widget para un tenant (nombre, bienvenida, branding).")
            .Produces<WebChatConfigResponse>();

        return app;
    }

    private static async Task<IResult> GetConfigAsync(
        HttpContext context, GlobalDbContext globalDb, CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();
        var config   = context.GetTenantConfiguration();

        var name = await globalDb.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var custom = config?.ChannelCustomization;
        return Results.Ok(new WebChatConfigResponse(
            name,
            custom?.WelcomeMessage ?? "Hola, ¿en qué te podemos ayudar?",
            custom?.PrimaryColor,
            custom?.LogoUrl));
    }
}
