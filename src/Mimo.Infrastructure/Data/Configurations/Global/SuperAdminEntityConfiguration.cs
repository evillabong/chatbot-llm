using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Global;

/// <summary>
/// Configuración EF Core de la entidad SuperAdmin.
/// Se persiste en el esquema public (catálogo global de la plataforma).
/// </summary>
public class SuperAdminEntityConfiguration : IEntityTypeConfiguration<SuperAdmin>
{
    public void Configure(EntityTypeBuilder<SuperAdmin> b)
    {
        b.ToTable("super_admins", "public");

        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(s => s.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        b.HasIndex(s => s.Email)
            .IsUnique()
            .HasDatabaseName("ix_super_admins_email");

        b.Property(s => s.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(s => s.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        b.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
    }
}
