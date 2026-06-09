using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Agent;

/// <summary>
/// Datos para crear un nuevo funcionario en un tenant.
/// </summary>
public record CreateAgentRequest(
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(200)] string FullName,
    [Required, MaxLength(100)] string Alias,
    [Range(1, 50)] int MaxConcurrentSessions = 5,
    IReadOnlyList<Guid>? RoleIds = null
);
