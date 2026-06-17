using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Global;

/// <summary>
/// Configuración EF Core de la entidad ApiKey (esquema public).
/// Indexada por KeyHash para la resolución rápida del tenant al autenticar.
/// </summary>
public class ApiKeyEntityConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.ToTable("api_keys", "public");

        b.HasKey(k => k.Id);
        b.Property(k => k.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(k => k.TenantId).HasColumnName("tenant_id").IsRequired();
        b.HasIndex(k => k.TenantId).HasDatabaseName("ix_api_keys_tenant_id");

        b.Property(k => k.Name).HasColumnName("name").HasMaxLength(120).IsRequired();

        b.Property(k => k.Prefix).HasColumnName("prefix").HasMaxLength(20).IsRequired();

        b.Property(k => k.KeyHash).HasColumnName("key_hash").HasMaxLength(100).IsRequired();
        b.HasIndex(k => k.KeyHash).IsUnique().HasDatabaseName("ix_api_keys_key_hash");

        b.Property(k => k.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(k => k.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(k => k.LastUsedAt).HasColumnName("last_used_at");
        b.Property(k => k.RevokedAt).HasColumnName("revoked_at");
    }
}
