using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Knowledge;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Lectura de señales de mejora continua (#22, Fase 1). Hoy expone los vacíos de conocimiento
/// (consultas con baja recuperación semántica) para que un TenantAdmin priorice qué documentar.
/// Es la base de la futura bandeja de curación de sugerencias.
/// </summary>
public static class KnowledgeInsightsEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/knowledge")
            .WithTags("KnowledgeInsights")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/gaps", GetGapsAsync)
            .WithName("ListKnowledgeGaps")
            .WithSummary("Lista los vacíos de conocimiento detectados más recientes (query opcional: limit).")
            .Produces<List<KnowledgeGapResponse>>();

        return app;
    }

    private static async Task<IResult> GetGapsAsync(
        IKnowledgeSignalRepository signals, int? limit = null, CancellationToken ct = default)
    {
        var take = Math.Clamp(limit ?? 50, 1, 200);
        var gaps = await signals.GetRecentGapsAsync(take, ct);
        var response = gaps
            .Select(s => new KnowledgeGapResponse(
                s.Id, s.ConversationId, s.QueryText, s.TopSimilarity ?? 0, s.MatchCount, s.CreatedAt))
            .ToList();
        return Results.Ok(response);
    }
}
