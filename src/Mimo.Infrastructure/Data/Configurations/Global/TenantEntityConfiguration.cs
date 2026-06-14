using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models.Configuration;
using System.Text.Json;

// Alias necesario: 'Tenant' como nombre corto se resuelve como el namespace hermano
// Mimo.Infrastructure.Data.Configurations.Tenant en lugar del tipo del modelo.
using TenantModel = Mimo.Core.Models.Tenant;

namespace Mimo.Infrastructure.Data.Configurations.Global;

/// <summary>
/// Configuración EF Core de la entidad Tenant.
/// Se persiste en el esquema public (catálogo global de la plataforma).
/// </summary>
public class TenantEntityConfiguration : IEntityTypeConfiguration<TenantModel>
{
    public void Configure(EntityTypeBuilder<TenantModel> b)
    {
        b.ToTable("tenants", "public");

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(t => t.Slug)
            .HasColumnName("slug")
            .HasMaxLength(50)
            .IsRequired();

        b.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("ix_tenants_slug");

        b.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        b.Property(t => t.Plan)
            .HasColumnName("plan")
            .HasMaxLength(50)
            .IsRequired();

        // FK al catálogo de planes por código (integridad referencial: el plan debe existir).
        // Sin propiedad de navegación: Tenant.Plan sigue siendo el código del plan.
        b.HasOne<Mimo.Core.Models.Plan>()
            .WithMany()
            .HasForeignKey(t => t.Plan)
            .HasPrincipalKey(p => p.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuración serializada como JSONB
        b.Property(t => t.Configuration)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<TenantConfiguration>(v, JsonSerializerOptions.Default) ?? new())
            .IsRequired();

        b.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
