using System.Text.Json;
using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Chatbot;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Api.Endpoints;

/// <summary>
/// Gestión de flujos guiados del chatbot por opciones (#27), bajo JWT/TenantAdmin. Si hay un flujo
/// activo, el orquestador conduce las conversaciones del bot por él (modo determinista, sin IA).
/// </summary>
public static class ChatbotFlowEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapChatbotFlowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/chatbot/flows")
            .WithTags("Chatbot")
            .RequireAuthorization(MimoAuthorization.Policies.TenantAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListChatbotFlows")
            .WithSummary("Lista los flujos guiados del tenant.")
            .Produces<List<ChatbotFlowResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateChatbotFlow")
            .WithSummary("Crea un flujo guiado (opcionalmente lo activa).")
            .Produces<ChatbotFlowResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/activate", ActivateAsync)
            .WithName("ActivateChatbotFlow")
            .WithSummary("Activa un flujo (desactiva los demás) (query: id).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(IChatbotFlowRepository repo, CancellationToken ct = default)
    {
        var flows = await repo.ListAsync(ct);
        return Results.Ok(flows.Select(f => new ChatbotFlowResponse(f.Id, f.Name, f.IsActive, f.CreatedAt)).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateChatbotFlowRequest request, IChatbotFlowRepository repo, CancellationToken ct = default)
    {
        var def = request.Definition;
        if (def.Nodes is null || def.Nodes.Count == 0)
            return Results.BadRequest(new { error = "El flujo debe tener al menos un nodo." });
        if (def.Nodes.All(n => n.Id != def.EntryNodeId))
            return Results.BadRequest(new { error = $"El nodo de entrada '{def.EntryNodeId}' no existe entre los nodos." });

        var flow = new ChatbotFlow
        {
            Id         = Guid.NewGuid(),
            Name       = request.Name,
            IsActive   = false,
            Definition = JsonSerializer.Serialize(def, JsonOptions),
            CreatedAt  = DateTime.UtcNow
        };
        await repo.AddAsync(flow, ct);

        if (request.Activate)
            await repo.SetActiveAsync(flow.Id, ct);

        return Results.Created("/chatbot/flows",
            new ChatbotFlowResponse(flow.Id, flow.Name, request.Activate, flow.CreatedAt));
    }

    private static async Task<IResult> ActivateAsync(
        Guid id, IChatbotFlowRepository repo, CancellationToken ct = default)
    {
        var ok = await repo.SetActiveAsync(id, ct);
        return ok ? Results.NoContent() : Results.NotFound(new { error = "Flujo no encontrado." });
    }
}
