using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Services;

/// <summary>
/// Reparte un evento de dominio a los consumidores internos: webhooks salientes (#29) y el motor de
/// automatización (#24). Cada consumidor es best-effort por su cuenta; aquí se invocan ambos para que
/// un evento active reglas y entregue webhooks de forma consistente.
/// </summary>
public sealed class DomainEventPublisher(
    IWebhookPublisher webhooks,
    IAutomationDispatcher automation) : IDomainEventPublisher
{
    public async Task PublishAsync(string eventType, object payload, CancellationToken ct = default)
    {
        await webhooks.PublishAsync(eventType, payload, ct);
        await automation.DispatchAsync(eventType, payload, ct);
    }
}
