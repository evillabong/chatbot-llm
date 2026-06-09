using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core del funcionario dentro del esquema de un tenant.
/// </summary>
public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> b)
    {
        b.ToTable("agents");

        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();

        b.Property(a => a.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        b.HasIndex(a => a.Email)
            .IsUnique()
            .HasDatabaseName("ix_agents_email");

        b.Property(a => a.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(a => a.Alias)
            .HasColumnName("alias")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        b.Property(a => a.MaxConcurrentSessions)
            .HasColumnName("max_concurrent_sessions")
            .HasDefaultValue(5);

        b.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
    }
}
