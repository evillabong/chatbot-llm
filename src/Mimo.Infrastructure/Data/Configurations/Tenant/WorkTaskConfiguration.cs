using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core de las tareas operativas (#24).</summary>
public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> b)
    {
        b.ToTable("tasks");

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(t => t.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        b.Property(t => t.Description).HasColumnName("description");
        b.Property(t => t.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        b.Property(t => t.AssignedAgentId).HasColumnName("assigned_agent_id");
        b.Property(t => t.DueAt).HasColumnName("due_at");
        b.Property(t => t.ConversationId).HasColumnName("conversation_id");
        b.Property(t => t.TicketId).HasColumnName("ticket_id");
        b.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        b.Property(t => t.CompletedAt).HasColumnName("completed_at");

        // Bandeja de tareas: por estado y por responsable, más recientes primero.
        b.HasIndex(t => new { t.Status, t.CreatedAt }).HasDatabaseName("ix_tasks_status_created");
        b.HasIndex(t => t.AssignedAgentId).HasDatabaseName("ix_tasks_assigned_agent");
    }
}
