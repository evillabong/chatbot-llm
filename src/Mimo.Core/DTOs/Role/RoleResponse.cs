namespace Mimo.Core.DTOs.Role;

/// <summary>
/// Representación de un rol para las respuestas de la API.
/// </summary>
public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    int PriorityLevel,
    bool CanViewAllTickets,
    bool IsActive,
    DateTime CreatedAt
);
