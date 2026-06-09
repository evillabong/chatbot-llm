namespace Mimo.Core.Models;

/// <summary>
/// Representa un rol o departamento dentro de un tenant (ej: Financiero, Legal, Atención General).
/// </summary>
public class Role
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Nivel de prioridad para el ordenamiento de colas.</summary>
    public int PriorityLevel { get; set; } = 0;

    /// <summary>Indica si los miembros de este rol pueden ver todos los tickets del tenant (ej: supervisores).</summary>
    public bool CanViewAllTickets { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AgentRole> AgentRoles { get; set; } = [];
}
