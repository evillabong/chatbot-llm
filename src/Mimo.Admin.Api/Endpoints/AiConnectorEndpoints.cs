using Mimo.Core.Authorization;
using Mimo.Core.DTOs.Ai;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;

namespace Mimo.Admin.Api.Endpoints;

/// <summary>
/// Administración de conectores de IA. Solo SuperAdmin.
/// Las respuestas NO exponen la API key (solo indican si está configurada).
/// </summary>
public static class AiConnectorEndpoints
{
    public static IEndpointRouteBuilder MapAiConnectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ai/connectors")
            .WithTags("AiConnectors")
            .RequireAuthorization(MimoAuthorization.Policies.SuperAdmin);

        group.MapGet("/", ListAsync)
            .WithName("ListAiConnectors").WithSummary("Lista los conectores de IA.")
            .Produces<List<AiConnectorResponse>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateAiConnector").WithSummary("Registra un nuevo conector de IA.")
            .Produces<AiConnectorResponse>(StatusCodes.Status201Created).Produces(StatusCodes.Status409Conflict);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateAiConnector").WithSummary("Actualiza nombre y configuración de un conector (query: id).")
            .Produces<AiConnectorResponse>().Produces(StatusCodes.Status404NotFound);

        group.MapPost("/activate", ActivateAsync)
            .WithName("ActivateAiConnector").WithSummary("Marca un conector como el activo de la plataforma (query: id).")
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(IAiConnectorRepository repo, CancellationToken ct = default)
        => Results.Ok((await repo.ListAsync(ct)).Select(ToResponse).ToList());

    private static async Task<IResult> CreateAsync(
        CreateAiConnectorRequest request, IAiConnectorRepository repo, ISecretProtector protector, CancellationToken ct = default)
    {
        var provider = request.Provider.Trim().ToLowerInvariant();
        if (await repo.ProviderExistsAsync(provider, ct))
            return Results.Conflict(new { error = $"Ya existe un conector para el proveedor '{provider}'." });

        var connector = new AiConnector
        {
            Id          = Guid.NewGuid(),
            Provider    = provider,
            DisplayName = request.DisplayName,
            IsActive    = false, // se activa explícitamente vía /activate
            Settings    = ToSettings(request.Settings, protector),
            CreatedAt   = DateTime.UtcNow
        };

        await repo.AddAsync(connector, ct);
        return Results.Created($"/ai/connectors", ToResponse(connector));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateAiConnectorRequest request, IAiConnectorRepository repo, ISecretProtector protector, CancellationToken ct = default)
    {
        var connector = await repo.GetByIdAsync(id, ct);
        if (connector is null) return Results.NotFound(new { error = "Conector no encontrado." });

        connector.DisplayName = request.DisplayName;
        connector.Settings    = ToSettings(request.Settings, protector);
        connector.UpdatedAt   = DateTime.UtcNow;

        await repo.UpdateAsync(connector, ct);
        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(connector));
    }

    private static async Task<IResult> ActivateAsync(Guid id, IAiConnectorRepository repo, CancellationToken ct = default)
    {
        var ok = await repo.SetActiveAsync(id, ct);
        return ok
            ? Results.NoContent()
            : Results.NotFound(new { error = "Conector no encontrado." });
    }

    private static LlmConnectorSettings ToSettings(AiConnectorSettingsDto dto, ISecretProtector protector) =>
        new()
        {
            ApiKey         = protector.Protect(dto.ApiKey),  // cifrada en reposo
            BaseUrl        = dto.BaseUrl,
            ChatModel      = dto.ChatModel,
            EmbeddingModel = dto.EmbeddingModel,
            Temperature    = dto.Temperature,
            MaxTokens      = dto.MaxTokens
        };

    private static AiConnectorResponse ToResponse(AiConnector c) =>
        new(c.Id, c.Provider, c.DisplayName, c.IsActive,
            c.Settings.BaseUrl, c.Settings.ChatModel, c.Settings.EmbeddingModel,
            c.Settings.Temperature, c.Settings.MaxTokens,
            HasApiKey: !string.IsNullOrEmpty(c.Settings.ApiKey),
            c.CreatedAt, c.UpdatedAt);
}
