using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de reglas de automatización (#24) dentro del esquema de un tenant.
/// </summary>
public class AutomationRuleRepository(TenantDbContext db) : IAutomationRuleRepository
{
    public Task<AutomationRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.AutomationRules.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<AutomationRule>> ListAsync(CancellationToken ct = default)
        => await db.AutomationRules.AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<AutomationRule>> ListEnabledByTriggerAsync(
        string triggerEvent, CancellationToken ct = default)
        => await db.AutomationRules.AsNoTracking()
            .Where(r => r.IsEnabled && r.TriggerEvent == triggerEvent)
            .ToListAsync(ct);

    public async Task<AutomationRule> AddAsync(AutomationRule rule, CancellationToken ct = default)
    {
        db.AutomationRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task RemoveAsync(AutomationRule rule, CancellationToken ct = default)
    {
        db.AutomationRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }
}
