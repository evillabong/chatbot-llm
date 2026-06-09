using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Auth;

/// <summary>
/// Datos para crear el primer super administrador de la plataforma.
/// El endpoint que consume este DTO solo opera si aún no existe ningún SuperAdmin.
/// </summary>
public record SetupSuperAdminRequest(
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(200)] string FullName
);
