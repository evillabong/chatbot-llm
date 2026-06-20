namespace Mimo.Core.DTOs.Auth;

/// <summary>
/// Respuesta al crear el primer super administrador (`POST /auth/setup`): identifica al SuperAdmin
/// recién creado. No incluye credenciales.
/// </summary>
public record SetupSuperAdminResponse(
    Guid Id,
    string Email
);
