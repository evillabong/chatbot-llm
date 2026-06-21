namespace Mimo.Core.Interfaces;

/// <summary>
/// Publica un evento de dominio y lo reparte a los consumidores internos: webhooks salientes (#29) y
/// el motor de automatización (#24). Es el punto único que usan los endpoints al ocurrir un evento.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(string eventType, object payload, CancellationToken ct = default);
}
