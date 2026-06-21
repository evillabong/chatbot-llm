using Microsoft.EntityFrameworkCore;
using Mimo.Api.Authentication;
using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Conversation;
using Mimo.Core.DTOs.Integration;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Webhooks;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Superficie PÚBLICA de interoperabilidad (corte 2): subconjunto curado de endpoints para que los
/// sistemas del cliente (ERP, CRM, e-commerce…) se integren. Autenticados por API key vía la política
/// <see cref="MimoAuthorization.Policies.Integration"/> (cabecera <c>X-Api-Key</c>, no JWT). El tenant
/// se resuelve a partir de la clave (ver ADR 0015); el esquema lo fija TenantResolutionMiddleware.
///
/// Versionado en la ruta (/integration/v1) para poder evolucionar el contrato sin romper integraciones.
/// La gestión de las claves (alta/baja) está en <see cref="IntegrationEndpoints"/> bajo JWT/TenantAdmin.
/// </summary>
public static class IntegrationApiEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/integration/v1")
            .WithTags("Integration API")
            .RequireAuthorization(MimoAuthorization.Policies.Integration);

        group.MapGet("/me", GetIdentityAsync)
            .WithName("IntegrationIdentity")
            .WithSummary("Devuelve la organización y la API key con que se autenticó la petición.")
            .Produces<IntegrationIdentityResponse>();

        group.MapPost("/conversations", StartConversationAsync)
            .WithName("IntegrationStartConversation")
            .WithSummary("Inicia (o retoma) una conversación para un usuario externo.")
            .Produces<ConversationResponse>(StatusCodes.Status201Created);

        group.MapGet("/conversations/detail", GetConversationAsync)
            .WithName("IntegrationGetConversation")
            .WithSummary("Obtiene el estado y los mensajes recientes de una conversación (query: id).")
            .Produces<ConversationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> GetIdentityAsync(
        HttpContext context, GlobalDbContext globalDb, CancellationToken ct = default)
    {
        var tenantId   = context.GetTenantId();
        var apiKeyName = context.User.FindFirst(ApiKeyAuthenticationHandler.Claims.ApiKeyName)?.Value ?? string.Empty;

        var tenant = await globalDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
            return Results.NotFound(new { error = "Organización no encontrada." });

        return Results.Ok(new IntegrationIdentityResponse(tenant.Slug, tenant.Name, tenant.Plan, apiKeyName));
    }

    private static async Task<IResult> StartConversationAsync(
        StartConversationRequest request,
        IConversationRepository repo,
        IDomainEventPublisher webhooks,
        HttpContext context,
        CancellationToken ct = default)
    {
        var tenantId = context.GetTenantId();

        // Reutilizar la conversación activa del mismo usuario/canal si ya existe.
        var existing = await repo.GetByExternalUserAsync(request.ExternalUserId, request.Channel, ct);
        if (existing is not null)
            return Results.Ok(ToResponse(existing, []));

        var conversation = new Conversation
        {
            Id               = Guid.NewGuid(),
            TenantId         = tenantId,
            ExternalUserId   = request.ExternalUserId,
            ExternalUserName = request.ExternalUserName,
            Channel          = request.Channel,
            Status           = TicketStatus.BotActive,
            CreatedAt        = DateTime.UtcNow,
            LastMessageAt    = DateTime.UtcNow
        };

        await repo.AddAsync(conversation, ct);
        await webhooks.PublishAsync(WebhookEventTypes.ConversationCreated, new
        {
            id               = conversation.Id,
            channel          = conversation.Channel.ToString(),
            externalUserId   = conversation.ExternalUserId,
            externalUserName = conversation.ExternalUserName,
            createdAt        = conversation.CreatedAt
        }, ct);

        return Results.Created($"/integration/v1/conversations/detail?id={conversation.Id}", ToResponse(conversation, []));
    }

    private static async Task<IResult> GetConversationAsync(
        Guid id, IConversationRepository repo, CancellationToken ct = default)
    {
        var conversation = await repo.GetByIdAsync(id, ct);
        if (conversation is null)
            return Results.NotFound(new { error = "Conversación no encontrada." });

        var messages = await repo.GetMessagesAsync(id, limit: 50, ct);
        return Results.Ok(ToResponse(conversation, messages));
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static ConversationResponse ToResponse(Conversation c, IReadOnlyList<Message> messages) =>
        new(c.Id, c.Channel, c.Status, c.IsAuthenticated, c.CreatedAt,
            messages.Select(m => new MessageResponse(m.Id, m.Role, m.Content, m.CreatedAt)).ToList(),
            c.ExternalUserName, c.CustomerEmail, c.CustomerPhone, c.LastMessageAt);
}
