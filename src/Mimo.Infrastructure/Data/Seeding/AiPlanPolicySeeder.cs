using Microsoft.EntityFrameworkCore;
using Mimo.Core.Constants;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// Siembra políticas de IA por plan (cuotas + proveedores permitidos) para que las
/// restricciones por plan tengan datos de partida. Idempotente por código de plan.
/// </summary>
public static class AiPlanPolicySeeder
{
    public static async Task SeedDevAsync(GlobalDbContext db, CancellationToken ct = default)
    {
        await EnsureAsync(db, "free", monthlyRequests: 1_000,  monthlyTokens: 500_000,    ct);
        await EnsureAsync(db, "pro",  monthlyRequests: 50_000, monthlyTokens: 25_000_000, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureAsync(
        GlobalDbContext db, string planCode, int monthlyRequests, long monthlyTokens, CancellationToken ct)
    {
        if (await db.AiPlanPolicies.AnyAsync(p => p.PlanCode == planCode, ct))
            return;

        db.AiPlanPolicies.Add(new AiPlanPolicy
        {
            Id                  = Guid.NewGuid(),
            PlanCode            = planCode,
            AllowedProviders    = [AiProviders.DeepSeek],
            MonthlyRequestQuota = monthlyRequests,
            MonthlyTokenQuota   = monthlyTokens,
            IsActive            = true,
            CreatedAt           = DateTime.UtcNow
        });
    }
}
