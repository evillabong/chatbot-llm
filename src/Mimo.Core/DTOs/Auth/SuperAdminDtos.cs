using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Auth;

/// <summary>Solicitud autenticada para crear un super administrador adicional (#10).</summary>
public record CreateSuperAdminRequest(
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(200)] string FullName
);

/// <summary>Datos de un super administrador (sin credenciales).</summary>
public record SuperAdminResponse(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    DateTime CreatedAt
);
