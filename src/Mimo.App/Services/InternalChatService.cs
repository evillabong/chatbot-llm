using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Chat interno entre funcionarios (endpoint /internal-chat) por encima del cliente Kiota.
/// </summary>
public sealed class InternalChatService(MimoApiClient api)
{
    public async Task<List<InternalMessageResponse>> HistoryAsync(Guid agentId, CancellationToken ct = default)
    {
        var result = await api.InternalChat.History.GetAsync(rc => rc.QueryParameters.AgentId = agentId, ct);
        return result ?? [];
    }

    public Task<InternalMessageResponse?> SendAsync(Guid toAgentId, string content, CancellationToken ct = default) =>
        api.InternalChat.Send.PostAsync(
            new SendInternalMessageRequest { ToAgentId = toAgentId, Content = content }, cancellationToken: ct);

    public Task MarkReadAsync(Guid agentId, CancellationToken ct = default) =>
        api.InternalChat.Read.PostAsync(rc => rc.QueryParameters.AgentId = agentId, ct);
}
