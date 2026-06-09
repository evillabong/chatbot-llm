namespace Mimo.Core.Models;

/// <summary>
/// Tabla pivote entre funcionario y rol. Un funcionario puede pertenecer a varios roles.
/// </summary>
public class AgentRole
{
    public Guid AgentId { get; set; }
    public Agent Agent { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
