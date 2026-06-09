using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Role;

/// <summary>
/// Datos para crear un nuevo rol/departamento en un tenant.
/// </summary>
public record CreateRoleRequest(
    [Required, MaxLength(100)] string Name,
    string? Description = null,
    int PriorityLevel = 0,
    bool CanViewAllTickets = false
);
