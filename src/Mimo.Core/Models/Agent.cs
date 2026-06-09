namespace Mimo.Core.Models;

/// <summary>
/// Representa a un funcionario que atiende sesiones dentro de un tenant.
/// </summary>
public class Agent
{
    public Guid Id { get; set; }

    /// <summary>Tenant al que pertenece el funcionario.</summary>
    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña (PBKDF2). Nunca se expone en DTOs de respuesta.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Nombre real del funcionario (uso interno).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Alias visible para el ciudadano durante la atención.</summary>
    public string Alias { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Número máximo de sesiones simultáneas permitidas para este funcionario.</summary>
    public int MaxConcurrentSessions { get; set; } = 5;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Roles asignados al funcionario.</summary>
    public ICollection<AgentRole> AgentRoles { get; set; } = [];
}
