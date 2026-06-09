using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Auth;

/// <summary>
/// Credenciales para iniciar sesión (funcionario o super administrador).
/// </summary>
public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
