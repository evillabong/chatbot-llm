using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Global;

/// <summary>
/// Configuración EF Core del registro de uso de IA (esquema public, append-only).
/// Índice por (tenant_id, created_at) para agregación de estadísticas y cuotas por periodo.
/// </summary>
public class AiUsageRecordEntityConfiguration : IEntityTypeConfiguration<AiUsageRecord>
{
    public void Configure(EntityTypeBuilder<AiUsageRecord> b)
    {
        b.ToTable("ai_usage_records", "public");

        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(u => u.TenantId).HasColumnName("tenant_id").IsRequired();

        b.Property(u => u.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        b.Property(u => u.Model)
            .HasColumnName("model")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(u => u.Operation)
            .HasColumnName("operation")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        b.Property(u => u.PromptTokens).HasColumnName("prompt_tokens").HasDefaultValue(0);
        b.Property(u => u.CompletionTokens).HasColumnName("completion_tokens").HasDefaultValue(0);
        b.Property(u => u.TotalTokens).HasColumnName("total_tokens").HasDefaultValue(0);

        b.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.HasIndex(u => new { u.TenantId, u.CreatedAt })
            .HasDatabaseName("ix_ai_usage_records_tenant_created");
    }
}
