using Mimo.Api.Middleware;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Sales;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Sales;
using Mimo.Core.Webhooks;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Oportunidades de venta (#26, capacidad opcional de ventas/CRM). Gestión para cualquier funcionario:
/// crear, listar (filtro por etapa/responsable), actualizar (incluida la etapa, que fija/limpia el
/// cierre) y eliminar. El gating por plan se añadirá en un corte posterior.
/// </summary>
public static class OpportunityEndpoints
{
    public static IEndpointRouteBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/opportunities")
            .WithTags("Opportunities")
            .RequireAuthorization(MimoAuthorization.Policies.Agent);

        group.MapGet("/", ListAsync)
            .WithName("ListOpportunities")
            .WithSummary("Lista las oportunidades (query opcional: stage, assignedAgentId).")
            .Produces<List<OpportunityResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateOpportunity")
            .WithSummary("Crea una oportunidad de venta.")
            .Produces<OpportunityResponse>(StatusCodes.Status201Created);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateOpportunity")
            .WithSummary("Actualiza una oportunidad, incluida su etapa (query: id).")
            .Produces<OpportunityResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/", DeleteAsync)
            .WithName("DeleteOpportunity")
            .WithSummary("Elimina una oportunidad (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/sync", SyncAsync)
            .WithName("SyncOpportunity")
            .WithSummary("Sincroniza una oportunidad con el CRM externo (query: id).")
            .Produces<CrmSyncResultResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/sync-log", SyncLogAsync)
            .WithName("OpportunitySyncLog")
            .WithSummary("Bitácora de sincronización de una oportunidad con el CRM (query: id).")
            .Produces<List<CrmSyncLogResponse>>();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IOpportunityRepository repo, int? stage = null, Guid? assignedAgentId = null, CancellationToken ct = default)
    {
        OpportunityStage? filter = stage is not null && Enum.IsDefined(typeof(OpportunityStage), stage.Value)
            ? (OpportunityStage)stage.Value
            : null;

        var items = await repo.ListAsync(filter, assignedAgentId, ct);
        return Results.Ok(items.Select(ToResponse).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateOpportunityRequest request, IOpportunityRepository repo, IDomainEventPublisher events,
        HttpContext context, CancellationToken ct = default)
    {
        var opportunity = new Opportunity
        {
            Id              = Guid.NewGuid(),
            TenantId        = context.GetTenantId(),
            Title           = request.Title.Trim(),
            ContactName     = request.ContactName,
            ContactEmail    = request.ContactEmail,
            ContactPhone    = request.ContactPhone,
            Stage           = OpportunityStage.New,
            Amount          = request.Amount < 0 ? 0 : request.Amount,
            ConversationId  = request.ConversationId,
            AssignedAgentId = request.AssignedAgentId,
            Notes           = request.Notes,
            CreatedAt       = DateTime.UtcNow
        };
        await repo.AddAsync(opportunity, ct);

        await events.PublishAsync(WebhookEventTypes.OpportunityCreated, new
        {
            id = opportunity.Id,
            title = opportunity.Title,
            stage = opportunity.Stage.ToString(),
            amount = opportunity.Amount,
            assignedAgentId = opportunity.AssignedAgentId
        }, ct);

        return Results.Created("/opportunities", ToResponse(opportunity));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateOpportunityRequest request, IOpportunityRepository repo, IDomainEventPublisher events,
        CancellationToken ct = default)
    {
        var opportunity = await repo.GetByIdAsync(id, ct);
        if (opportunity is null)
            return Results.NotFound(new { error = "Oportunidad no encontrada." });

        var previousStage = opportunity.Stage;

        opportunity.Title           = request.Title.Trim();
        opportunity.ContactName     = request.ContactName;
        opportunity.ContactEmail    = request.ContactEmail;
        opportunity.ContactPhone    = request.ContactPhone;
        opportunity.Stage           = request.Stage;
        opportunity.Amount          = request.Amount < 0 ? 0 : request.Amount;
        opportunity.AssignedAgentId = request.AssignedAgentId;
        opportunity.Notes           = request.Notes;
        opportunity.ClosedAt        = OpportunityStageRules.ResolveClosedAt(request.Stage, opportunity.ClosedAt, DateTime.UtcNow);
        opportunity.UpdatedAt       = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        if (previousStage != opportunity.Stage)
        {
            await events.PublishAsync(WebhookEventTypes.OpportunityStageChanged, new
            {
                id = opportunity.Id,
                title = opportunity.Title,
                stage = opportunity.Stage.ToString(),
                previousStage = previousStage.ToString(),
                amount = opportunity.Amount,
                assignedAgentId = opportunity.AssignedAgentId
            }, ct);
        }

        return Results.Ok(ToResponse(opportunity));
    }

    private static async Task<IResult> DeleteAsync(Guid id, IOpportunityRepository repo, CancellationToken ct = default)
    {
        var opportunity = await repo.GetByIdAsync(id, ct);
        if (opportunity is null)
            return Results.NotFound(new { error = "Oportunidad no encontrada." });

        await repo.RemoveAsync(opportunity, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SyncAsync(
        Guid id, IOpportunityRepository repo, ICrmSyncService crmSync, CancellationToken ct = default)
    {
        if (await repo.GetByIdAsync(id, ct) is null)
            return Results.NotFound(new { error = "Oportunidad no encontrada." });

        var result = await crmSync.SyncOpportunityAsync(id, ct);
        return Results.Ok(new CrmSyncResultResponse(result.Success, result.ExternalId, result.Message));
    }

    private static async Task<IResult> SyncLogAsync(
        Guid id, ICrmSyncLogRepository logs, CancellationToken ct = default)
    {
        var items = await logs.ListByOpportunityAsync(id, 50, ct);
        var response = items
            .Select(l => new CrmSyncLogResponse(l.Id, l.Provider, l.Success, l.ExternalId, l.Message, l.CreatedAt))
            .ToList();
        return Results.Ok(response);
    }

    private static OpportunityResponse ToResponse(Opportunity o) => new(
        o.Id, o.Title, o.ContactName, o.ContactEmail, o.ContactPhone, o.Stage, o.Amount,
        o.ConversationId, o.AssignedAgentId, o.Notes, o.ExternalCrmId, o.LastSyncedAt,
        o.CreatedAt, o.UpdatedAt, o.ClosedAt);
}
