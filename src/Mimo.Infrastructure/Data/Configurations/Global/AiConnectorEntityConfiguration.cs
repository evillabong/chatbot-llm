using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;

namespace Mimo.Infrastructure.Data.Configurations.Global;

/// <summary>
/// Configuración EF Core de la entidad AiConnector.
/// Se persiste en el esquema public (catálogo global de la plataforma).
/// La configuración específica del proveedor se serializa como JSONB.
/// </summary>
public class AiConnectorEntityConfiguration : IEntityTypeConfiguration<AiConnector>
{
    public void Configure(EntityTypeBuilder<AiConnector> b)
    {
        b.ToTable("ai_connectors", "public");

        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(c => c.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        b.HasIndex(c => c.Provider)
            .IsUnique()
            .HasDatabaseName("ix_ai_connectors_provider");

        b.Property(c => c.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(false);

        // Configuración del proveedor serializada como JSONB.
        b.Property(c => c.Settings)
            .HasColumnName("settings")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<LlmConnectorSettings>(v, JsonSerializerOptions.Default) ?? new())
            .IsRequired();

        b.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
