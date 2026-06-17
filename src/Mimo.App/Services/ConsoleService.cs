using Mimo.ApiClient.MimoApi;
using Mimo.ApiClient.MimoApi.Models;
using Mimo.ApiClient.MimoApi.Tickets.Resolve;

namespace Mimo.App.Services;

/// <summary>
/// Operaciones de la consola de agente: bandeja de tickets (míos / del equipo),
/// detalle de conversación y acciones (reclamar, resolver, cerrar). Sobre el cliente Kiota.
/// </summary>
public sealed class ConsoleService(MimoApiClient api)
{
    public async Task<List<TicketResponse>> MyTicketsAsync(CancellationToken ct = default)
    {
        var result = await api.Tickets.My.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public async Task<List<TicketResponse>> VisibleTicketsAsync(CancellationToken ct = default)
    {
        var result = await api.Tickets.Visible.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<ConversationResponse?> ConversationAsync(Guid conversationId, CancellationToken ct = default) =>
        api.Conversations.Detail.GetAsync(rc => rc.QueryParameters.Id = conversationId, ct);

    public Task<TicketResponse?> ClaimAsync(Guid ticketId, CancellationToken ct = default) =>
        api.Tickets.Claim.PostAsync(rc => rc.QueryParameters.Id = ticketId, ct);

    public Task<TicketResponse?> ResolveAsync(Guid ticketId, CancellationToken ct = default) =>
        api.Tickets.Resolve.PostAsync(new ResolveRequestBuilder.ResolvePostRequestBody(),
            rc => rc.QueryParameters.Id = ticketId, ct);

    public Task<TicketResponse?> CloseAsync(Guid ticketId, CancellationToken ct = default) =>
        api.Tickets.Close.PostAsync(rc => rc.QueryParameters.Id = ticketId, ct);
}
