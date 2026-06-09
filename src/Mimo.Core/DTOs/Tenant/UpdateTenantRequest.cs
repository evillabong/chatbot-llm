using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Tenant;

/// <summary>
/// Campos actualizables de un tenant por el SuperAdmin.
/// </summary>
public record UpdateTenantRequest(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(50)] string Plan,
    bool IsActive
);
