using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core de las reglas de automatización (#24).</summary>
public class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> b)
    {
        b.ToTable("automation_rules");

        b.HasKey(r => r.Id);
        b.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(r => r.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        b.Property(r => r.TriggerEvent).HasColumnName("trigger_event").HasMaxLength(80).IsRequired();
        b.Property(r => r.ConditionsJson).HasColumnName("conditions_json").HasDefaultValueSql("'[]'");
        b.Property(r => r.ActionType).HasColumnName("action_type").HasConversion<int>().IsRequired();
        b.Property(r => r.ActionTaskTitle).HasColumnName("action_task_title").HasMaxLength(200);
        b.Property(r => r.ActionAssignedAgentId).HasColumnName("action_assigned_agent_id");
        b.Property(r => r.ActionEscalateReason).HasColumnName("action_escalate_reason").HasMaxLength(200);
        b.Property(r => r.IsEnabled).HasColumnName("is_enabled").IsRequired();
        b.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        // El dispatcher consulta reglas habilitadas por evento disparador.
        b.HasIndex(r => new { r.TriggerEvent, r.IsEnabled }).HasDatabaseName("ix_automation_rules_trigger_enabled");
    }
}
