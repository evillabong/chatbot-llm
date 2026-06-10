namespace Mimo.Core.Models;

/// <summary>
/// Datos del funcionario administrador inicial creado al aprovisionar un nuevo tenant.
/// El hash de la contraseña ya viene calculado (responsabilidad de quien invoca el aprovisionamiento).
/// </summary>
public record TenantAdminSeed(
    string Email,
    string PasswordHash,
    string FullName,
    string Alias
);
