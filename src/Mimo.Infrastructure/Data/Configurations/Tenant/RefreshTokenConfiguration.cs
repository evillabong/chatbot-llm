using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core de los refresh tokens de funcionarios (#9).</summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(t => t.AgentId).HasColumnName("agent_id").IsRequired();
        b.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        b.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
        b.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(t => t.RevokedAt).HasColumnName("revoked_at");

        // Búsqueda por hash al renovar/revocar.
        b.HasIndex(t => t.TokenHash).HasDatabaseName("ux_refresh_tokens_hash").IsUnique();
        b.HasIndex(t => t.AgentId).HasDatabaseName("ix_refresh_tokens_agent");
    }
}
