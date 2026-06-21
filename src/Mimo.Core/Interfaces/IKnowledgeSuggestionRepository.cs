using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a las sugerencias de conocimiento (#22, Fase 1 — curación) dentro del esquema de un tenant.
/// </summary>
public interface IKnowledgeSuggestionRepository
{
    Task<KnowledgeSuggestion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeSuggestion>> ListAsync(KnowledgeSuggestionStatus? status = null, CancellationToken ct = default);
    Task<KnowledgeSuggestion> AddAsync(KnowledgeSuggestion suggestion, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
