using Microsoft.EntityFrameworkCore;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// Siembra el catálogo de planes por defecto si está vacío, para que el aprovisionamiento
/// de tenants funcione de inmediato. El SuperAdmin puede ampliar/editar el catálogo después.
/// </summary>
public static class PlanSeeder
{
    public static async Task SeedDefaultAsync(GlobalDbContext db, CancellationToken ct = default)
    {
        if (await db.Plans.AnyAsync(ct))
            return;

        db.Plans.AddRange(
            new Plan { Id = Guid.NewGuid(), Code = "free", Name = "Gratis",
                       Description = "Plan básico con límites reducidos.", IsActive = true },
            new Plan { Id = Guid.NewGuid(), Code = "pro", Name = "Pro",
                       Description = "Plan profesional con mayores límites.", IsActive = true });

        await db.SaveChangesAsync(ct);
    }
}
