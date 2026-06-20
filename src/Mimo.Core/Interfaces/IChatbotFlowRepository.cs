using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a los flujos guiados del chatbot por opciones (#27) dentro del esquema del tenant activo.
/// </summary>
public interface IChatbotFlowRepository
{
    /// <summary>El flujo activo del tenant (el que conduce las conversaciones en modo opciones), si hay.</summary>
    Task<ChatbotFlow?> GetActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ChatbotFlow>> ListAsync(CancellationToken ct = default);

    Task<ChatbotFlow> AddAsync(ChatbotFlow flow, CancellationToken ct = default);

    /// <summary>Activa el flujo indicado y desactiva los demás del tenant (solo uno activo).</summary>
    Task<bool> SetActiveAsync(Guid id, CancellationToken ct = default);
}
