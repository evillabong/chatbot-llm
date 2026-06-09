using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core del rol/departamento dentro del esquema de un tenant.
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");

        b.HasKey(r => r.Id);
        b.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();

        b.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(r => r.Description).HasColumnName("description");

        b.Property(r => r.PriorityLevel)
            .HasColumnName("priority_level")
            .HasDefaultValue(0);

        b.Property(r => r.CanViewAllTickets)
            .HasColumnName("can_view_all_tickets")
            .HasDefaultValue(false);

        b.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        b.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
    }
}
