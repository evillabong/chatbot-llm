using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core del registro de auditoría de transferencias de tickets.
/// </summary>
public class TransferRecordConfiguration : IEntityTypeConfiguration<TransferRecord>
{
    public void Configure(EntityTypeBuilder<TransferRecord> b)
    {
        b.ToTable("transfer_records");

        b.HasKey(tr => tr.Id);
        b.Property(tr => tr.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(tr => tr.ConversationId).HasColumnName("conversation_id").IsRequired();
        // TicketId queda como columna informativa (sin FK): el ticket de origen se elimina al transferir.
        b.Property(tr => tr.TicketId).HasColumnName("ticket_id").IsRequired();
        b.Property(tr => tr.FromRoleId).HasColumnName("from_role_id");
        b.Property(tr => tr.FromAgentId).HasColumnName("from_agent_id");
        b.Property(tr => tr.ToRoleId).HasColumnName("to_role_id").IsRequired();
        b.Property(tr => tr.ToAgentId).HasColumnName("to_agent_id");
        b.Property(tr => tr.TransferredBy).HasColumnName("transferred_by").IsRequired();
        b.Property(tr => tr.Reason).HasColumnName("reason");
        b.Property(tr => tr.IsPartial).HasColumnName("is_partial").HasDefaultValue(false);
        b.Property(tr => tr.ContextNote).HasColumnName("context_note");

        b.Property(tr => tr.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        // FK al CONVERSATION (lifetime correcto del historial); sin navegación inversa.
        b.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(tr => tr.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(tr => tr.ConversationId).HasDatabaseName("ix_transfer_records_conversation");
    }
}
