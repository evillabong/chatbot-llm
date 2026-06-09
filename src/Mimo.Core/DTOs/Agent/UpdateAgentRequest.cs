using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Agent;

/// <summary>
/// Datos actualizables de un funcionario.
/// </summary>
public record UpdateAgentRequest(
    [Required, MaxLength(200)] string FullName,
    [Required, MaxLength(100)] string Alias,
    [Range(1, 50)] int MaxConcurrentSessions,
    bool IsActive,
    IReadOnlyList<Guid>? RoleIds = null
);
