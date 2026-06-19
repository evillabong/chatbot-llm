using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Administración de webhooks salientes del tenant (endpoint /integration/webhooks).
/// El secreto de firma solo llega en la respuesta de creación.
/// </summary>
public sealed class WebhooksService(MimoApiClient api)
{
    public async Task<List<WebhookSubscriptionResponse>> ListAsync(CancellationToken ct = default)
    {
        var result = await api.Integration.Webhooks.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<CreatedWebhookSubscriptionResponse?> CreateAsync(
        string name, string url, List<string> events, CancellationToken ct = default) =>
        api.Integration.Webhooks.PostAsync(
            new CreateWebhookSubscriptionRequest { Name = name, Url = url, Events = events },
            cancellationToken: ct);

    public Task RevokeAsync(Guid id, CancellationToken ct = default) =>
        api.Integration.Webhooks.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);

    public async Task<List<WebhookDeliveryResponse>> ListDeliveriesAsync(CancellationToken ct = default)
    {
        var result = await api.Integration.Webhooks.Deliveries.GetAsync(cancellationToken: ct);
        return result ?? [];
    }
}
