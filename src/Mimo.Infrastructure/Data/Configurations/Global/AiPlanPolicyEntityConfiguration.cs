using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Global;

/// <summary>
/// Configuración EF Core de la política de IA por plan (esquema public).
/// La lista de proveedores permitidos se persiste como JSONB.
/// </summary>
public class AiPlanPolicyEntityConfiguration : IEntityTypeConfiguration<AiPlanPolicy>
{
    public void Configure(EntityTypeBuilder<AiPlanPolicy> b)
    {
        b.ToTable("ai_plan_policies", "public");

        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(p => p.PlanCode)
            .HasColumnName("plan_code")
            .HasMaxLength(50)
            .IsRequired();

        b.HasIndex(p => p.PlanCode)
            .IsUnique()
            .HasDatabaseName("ix_ai_plan_policies_plan_code");

        b.Property(p => p.AllowedProviders)
            .HasColumnName("allowed_providers")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new())
            .IsRequired();

        b.Property(p => p.MonthlyRequestQuota)
            .HasColumnName("monthly_request_quota")
            .HasDefaultValue(0);

        b.Property(p => p.MonthlyTokenQuota)
            .HasColumnName("monthly_token_quota")
            .HasDefaultValue(0L);

        b.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        b.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
