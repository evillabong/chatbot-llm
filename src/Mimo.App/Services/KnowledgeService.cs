using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Mejora continua (#22): vacíos de conocimiento (lectura) y bandeja de sugerencias (curación HITL)
/// por encima del cliente Kiota.
/// </summary>
public sealed class KnowledgeService(MimoApiClient api)
{
    public async Task<List<KnowledgeGapResponse>> ListGapsAsync(int? limit = null, CancellationToken ct = default)
    {
        var result = await api.Knowledge.Gaps.GetAsync(rc =>
        {
            if (limit is not null) rc.QueryParameters.Limit = limit;
        }, ct);
        return result ?? [];
    }

    public async Task<List<KnowledgeSuggestionResponse>> ListSuggestionsAsync(int? status = null, CancellationToken ct = default)
    {
        var result = await api.Knowledge.Suggestions.GetAsync(rc =>
        {
            if (status is not null) rc.QueryParameters.Status = status;
        }, ct);
        return result ?? [];
    }

    public Task<KnowledgeSuggestionResponse?> CreateSuggestionAsync(
        string draftTitle, string draftContent, Guid? sourceSignalId = null, CancellationToken ct = default) =>
        api.Knowledge.Suggestions.PostAsync(new CreateKnowledgeSuggestionRequest
        {
            DraftTitle     = draftTitle,
            DraftContent   = draftContent,
            SourceSignalId = sourceSignalId
        }, cancellationToken: ct);

    public Task<KnowledgeSuggestionResponse?> ApproveAsync(Guid id, CancellationToken ct = default) =>
        api.Knowledge.Suggestions.Approve.PostAsync(rc => rc.QueryParameters.Id = id, ct);

    public Task DiscardAsync(Guid id, CancellationToken ct = default) =>
        api.Knowledge.Suggestions.Discard.PostAsync(rc => rc.QueryParameters.Id = id, ct);
}
