namespace Mimo.Core.Models;

/// <summary>
/// Administrador de la plataforma MIMO con acceso a Mimo.Admin.Api.
/// Se persiste en el esquema public (catálogo global), independiente de cualquier tenant.
/// </summary>
public class SuperAdmin
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña (PBKDF2). Nunca se expone en DTOs de respuesta.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
