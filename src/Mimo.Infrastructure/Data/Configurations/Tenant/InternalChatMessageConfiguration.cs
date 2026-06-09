using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core del mensaje de chat interno entre funcionarios.
/// </summary>
public class InternalChatMessageConfiguration : IEntityTypeConfiguration<InternalChatMessage>
{
    public void Configure(EntityTypeBuilder<InternalChatMessage> b)
    {
        b.ToTable("internal_chat_messages");

        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(m => m.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(m => m.FromAgentId).HasColumnName("from_agent_id").IsRequired();
        b.Property(m => m.ToAgentId).HasColumnName("to_agent_id").IsRequired();
        b.Property(m => m.RelatedTicketId).HasColumnName("related_ticket_id");

        b.Property(m => m.Content).HasColumnName("content").IsRequired();

        b.Property(m => m.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false);

        b.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.HasIndex(m => new { m.ToAgentId, m.IsRead })
            .HasDatabaseName("ix_internal_chat_to_agent_unread");
    }
}
