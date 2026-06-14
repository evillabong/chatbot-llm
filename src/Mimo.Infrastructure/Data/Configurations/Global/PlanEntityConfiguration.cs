using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Global;

/// <summary>
/// Configuración EF Core del catálogo de planes (esquema public).
/// El código es clave alternativa única, referenciada por Tenant y AiPlanPolicy.
/// </summary>
public class PlanEntityConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("plans", "public");

        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        // Clave alternativa única: destino de las FK de tenants y políticas de IA.
        b.HasAlternateKey(p => p.Code);

        b.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(p => p.Description).HasColumnName("description");

        b.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        b.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.Property(p => p.UpdatedAt).HasColumnName("updated_at");
    }
}
