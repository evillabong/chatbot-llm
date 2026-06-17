using Microsoft.EntityFrameworkCore;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// Siembra un SuperAdmin de DESARROLLO con credenciales conocidas para poder entrar a
/// Mimo.Admin.Api sin pasar por el bootstrap /auth/setup. Idempotente por email: no toca
/// SuperAdmins existentes ni cambia contraseñas ya establecidas.
/// </summary>
public static class SuperAdminSeeder
{
    public static async Task SeedDevAsync(
        GlobalDbContext db, IPasswordHasher passwordHasher, CancellationToken ct = default)
    {
        if (await db.SuperAdmins.AnyAsync(s => s.Email == DevSeedDefaults.SuperAdminEmail, ct))
            return;

        db.SuperAdmins.Add(new SuperAdmin
        {
            Id           = Guid.NewGuid(),
            Email        = DevSeedDefaults.SuperAdminEmail,
            FullName     = "Dev SuperAdmin",
            PasswordHash = passwordHasher.Hash(DevSeedDefaults.Password),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
