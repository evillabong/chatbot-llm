using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Acceso a las reglas de automatización (#24) dentro del esquema de un tenant.
/// </summary>
public interface IAutomationRuleRepository
{
    Task<AutomationRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AutomationRule>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AutomationRule>> ListEnabledByTriggerAsync(string triggerEvent, CancellationToken ct = default);
    Task<AutomationRule> AddAsync(AutomationRule rule, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task RemoveAsync(AutomationRule rule, CancellationToken ct = default);
}
