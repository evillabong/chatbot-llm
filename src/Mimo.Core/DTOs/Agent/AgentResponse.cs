namespace Mimo.Core.DTOs.Agent;

/// <summary>
/// Representación de un funcionario para las respuestas de la API.
/// </summary>
public record AgentResponse(
    Guid Id,
    string Email,
    string FullName,
    string Alias,
    bool IsActive,
    int MaxConcurrentSessions,
    IReadOnlyList<RoleSummary> Roles,
    DateTime CreatedAt
);

/// <summary>Resumen de rol para incluir en la respuesta del funcionario.</summary>
public record RoleSummary(Guid Id, string Name);
