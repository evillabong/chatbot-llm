using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core del ticket de atención humana.
/// </summary>
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> b)
    {
        b.ToTable("tickets");

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(t => t.ConversationId).HasColumnName("conversation_id").IsRequired();

        b.Property(t => t.AssignedRoleId).HasColumnName("assigned_role_id").IsRequired();
        b.Property(t => t.AssignedAgentId).HasColumnName("assigned_agent_id");

        b.Property(t => t.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TicketPriority.Normal)
            .IsRequired();

        b.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TicketStatus.InQueue)
            .IsRequired();

        b.Property(t => t.InternalNotes).HasColumnName("internal_notes");
        b.Property(t => t.EscalationReason).HasColumnName("escalation_reason");

        b.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.Property(t => t.AssignedAt).HasColumnName("assigned_at");
        b.Property(t => t.FirstResponseAt).HasColumnName("first_response_at");
        b.Property(t => t.ResolvedAt).HasColumnName("resolved_at");

        // El historial de transferencias se ancla a la conversación (TransferRecordConfiguration),
        // no al ticket: el ticket de origen se reemplaza al transferir y se perdía el historial (#21b).

        b.HasOne(t => t.Survey)
            .WithOne(s => s.Ticket)
            .HasForeignKey<SatisfactionSurvey>(s => s.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(t => new { t.Status, t.AssignedRoleId })
            .HasDatabaseName("ix_tickets_status_role");

        b.HasIndex(t => t.AssignedAgentId)
            .HasDatabaseName("ix_tickets_assigned_agent");
    }
}
