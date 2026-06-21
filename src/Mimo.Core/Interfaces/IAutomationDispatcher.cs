namespace Mimo.Core.Interfaces;

/// <summary>
/// Evalúa las reglas de automatización (#24) ante un evento de dominio y ejecuta sus acciones.
/// Best-effort: un fallo nunca debe afectar la operación que originó el evento.
/// </summary>
public interface IAutomationDispatcher
{
    Task DispatchAsync(string eventType, object payload, CancellationToken ct = default);
}
