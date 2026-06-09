using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Role;

/// <summary>
/// Datos actualizables de un rol.
/// </summary>
public record UpdateRoleRequest(
    [Required, MaxLength(100)] string Name,
    string? Description,
    int PriorityLevel,
    bool CanViewAllTickets,
    bool IsActive
);
