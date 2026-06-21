using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a las señales de recuperación semántica (#22, Fase 1) dentro del esquema de un tenant.
/// </summary>
public interface IKnowledgeSignalRepository
{
    /// <summary>Registra una señal de consulta (best-effort; no debe romper el flujo de chat).</summary>
    Task AddAsync(KnowledgeQuerySignal signal, CancellationToken ct = default);

    /// <summary>Devuelve los vacíos de conocimiento más recientes (knowledge_gap = true).</summary>
    Task<IReadOnlyList<KnowledgeQuerySignal>> GetRecentGapsAsync(int limit = 50, CancellationToken ct = default);
}
