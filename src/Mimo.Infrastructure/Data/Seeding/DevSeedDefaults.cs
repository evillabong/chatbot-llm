namespace Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// Valores por defecto de los datos semilla de DESARROLLO. No son secretos de producción:
/// solo se siembran cuando el entorno es Development, igual que la contraseña local de Postgres.
/// Las cuentas reales se crean por los flujos normales (setup/aprovisionamiento) en otros entornos.
/// </summary>
public static class DevSeedDefaults
{
    /// <summary>Contraseña común de todas las cuentas de demo (cumple el mínimo de 8 caracteres).</summary>
    public const string Password = "Mimo123$";

    /// <summary>SuperAdmin de demo para Mimo.Admin.Api.</summary>
    public const string SuperAdminEmail = "dev@mimo.local";
}
