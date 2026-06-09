using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core de la tabla pivote entre funcionario y rol.
/// </summary>
public class AgentRoleConfiguration : IEntityTypeConfiguration<AgentRole>
{
    public void Configure(EntityTypeBuilder<AgentRole> b)
    {
        b.ToTable("agent_roles");

        b.HasKey(ar => new { ar.AgentId, ar.RoleId });
        b.Property(ar => ar.AgentId).HasColumnName("agent_id");
        b.Property(ar => ar.RoleId).HasColumnName("role_id");

        b.HasOne(ar => ar.Agent)
            .WithMany(a => a.AgentRoles)
            .HasForeignKey(ar => ar.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(ar => ar.Role)
            .WithMany(r => r.AgentRoles)
            .HasForeignKey(ar => ar.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
