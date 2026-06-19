using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mimo.Api.Middleware;
using Mimo.Core.DTOs.Survey;
using Mimo.Core.Enums;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Webhooks;
using Mimo.Infrastructure.Data;
using TenantModel = Mimo.Core.Models.Tenant;

namespace Mimo.Api.Endpoints;

/// <summary>Endpoints de encuesta de satisfacción post-atención.</summary>
public static class SurveyEndpoints
{
    public static IEndpointRouteBuilder MapSurveyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations/survey")
            .WithTags("Survey");

        group.MapPost("/", SubmitSurveyAsync)
            .WithName("SubmitSurvey")
            .WithSummary("Registra la encuesta de satisfacción del ciudadano (query: conversationId).");

        group.MapGet("/", GetSurveyAsync)
            .WithName("GetSurvey")
            .WithSummary("Obtiene la encuesta registrada de una conversación (query: conversationId).")
            .Produces<SurveyResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> SubmitSurveyAsync(
        Guid conversationId,
        [FromBody] SubmitSurveyRequest request,
        TenantDbContext db,
        GlobalDbContext globalDb,
        IWebhookPublisher webhooks,
        HttpContext context,
        CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5)
            return Results.BadRequest("La calificación debe estar entre 1 y 5.");

        var tenantId = context.GetTenantId();

        // Verificar configuración del survey en el tenant
        var tenant = await globalDb.Tenants.FindAsync(new object[] { tenantId }, ct);
        var surveyConfig = tenant?.Configuration?.SatisfactionSurvey;
        if (surveyConfig is { SurveyEnabled: false })
            return Results.BadRequest("Las encuestas de satisfacción no están habilitadas.");

        // Buscar conversación con su ticket y survey existente
        var conversation = await db.Conversations
            .Include(c => c.Ticket)
                .ThenInclude(t => t!.Survey)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return Results.NotFound("Conversación no encontrada.");

        if (conversation.Ticket is null)
            return Results.BadRequest("Esta conversación no tiene un ticket asociado.");

        var ticket = conversation.Ticket;

        // Solo se puede enviar encuesta cuando el ticket está en Survey o Resolved
        if (ticket.Status is not (TicketStatus.Survey or TicketStatus.Resolved))
            return Results.BadRequest("El ticket no está en estado de encuesta.");

        if (ticket.Survey is not null)
            return Results.Conflict("Ya existe una encuesta registrada para este ticket.");

        // Observaciones: solo si el tenant lo permite
        var observations = (surveyConfig?.AllowObservations ?? true) ? request.Observations : null;

        // Registrar la encuesta
        var survey = new SatisfactionSurvey
        {
            Id           = Guid.NewGuid(),
            TicketId     = ticket.Id,
            Rating       = request.Rating,
            Observations = observations,
            RecordedAt   = DateTime.UtcNow
        };

        db.SatisfactionSurveys.Add(survey);

        // Determinar estado final del ticket
        var shouldReopen = surveyConfig is { ReopenOnLowRating: true }
                        && request.Rating < surveyConfig.ReopenRatingThreshold;

        ticket.Status = shouldReopen ? TicketStatus.Reopened : TicketStatus.Closed;

        await db.SaveChangesAsync(ct);

        await webhooks.PublishAsync(WebhookEventTypes.SurveyRecorded, new
        {
            surveyId       = survey.Id,
            ticketId       = survey.TicketId,
            conversationId = conversationId,
            rating         = survey.Rating,
            observations   = survey.Observations,
            recordedAt     = survey.RecordedAt
        }, ct);

        var response = new SurveyResponse(
            survey.Id,
            survey.TicketId,
            survey.Rating,
            survey.Observations,
            survey.RecordedAt);

        return Results.Created($"/conversations/survey?conversationId={conversationId}", response);
    }

    private static async Task<IResult> GetSurveyAsync(
        Guid conversationId,
        TenantDbContext db,
        CancellationToken ct)
    {
        var survey = await db.Conversations
            .Where(c => c.Id == conversationId)
            .Select(c => c.Ticket!.Survey)
            .FirstOrDefaultAsync(ct);

        if (survey is null)
            return Results.NotFound("No hay encuesta registrada para esta conversación.");

        return Results.Ok(new SurveyResponse(
            survey.Id,
            survey.TicketId,
            survey.Rating,
            survey.Observations,
            survey.RecordedAt));
    }
}
