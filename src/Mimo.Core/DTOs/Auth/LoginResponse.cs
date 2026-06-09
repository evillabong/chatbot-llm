namespace Mimo.Core.DTOs.Auth;

/// <summary>
/// Respuesta de autenticación exitosa: token JWT y datos básicos del usuario autenticado.
/// </summary>
public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles
);
