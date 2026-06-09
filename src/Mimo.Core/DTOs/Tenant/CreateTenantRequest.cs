using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Tenant;

/// <summary>
/// Datos requeridos para registrar un nuevo tenant en la plataforma.
/// Solo el SuperAdmin puede crear tenants.
/// </summary>
public record CreateTenantRequest(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(50), RegularExpression(@"^[a-z0-9\-]+$",
        ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones.")]
    string Slug,
    [Required, MaxLength(50)] string Plan
);
