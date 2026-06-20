using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Repositories;

/// <summary>
/// Repositorio de flujos guiados del chatbot por opciones (#27) dentro del esquema del tenant.
/// </summary>
public class ChatbotFlowRepository(TenantDbContext db) : IChatbotFlowRepository
{
    public Task<ChatbotFlow?> GetActiveAsync(CancellationToken ct = default)
        => db.ChatbotFlows.AsNoTracking().FirstOrDefaultAsync(f => f.IsActive, ct);

    public async Task<IReadOnlyList<ChatbotFlow>> ListAsync(CancellationToken ct = default)
        => await db.ChatbotFlows.AsNoTracking().OrderByDescending(f => f.CreatedAt).ToListAsync(ct);

    public async Task<ChatbotFlow> AddAsync(ChatbotFlow flow, CancellationToken ct = default)
    {
        db.ChatbotFlows.Add(flow);
        await db.SaveChangesAsync(ct);
        return flow;
    }

    public async Task<bool> SetActiveAsync(Guid id, CancellationToken ct = default)
    {
        var target = await db.ChatbotFlows.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (target is null) return false;

        // Solo un flujo activo por tenant: desactivar los demás.
        await db.ChatbotFlows.Where(f => f.IsActive && f.Id != id)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsActive, false), ct);

        if (!target.IsActive)
        {
            target.IsActive = true;
            await db.SaveChangesAsync(ct);
        }
        return true;
    }
}
