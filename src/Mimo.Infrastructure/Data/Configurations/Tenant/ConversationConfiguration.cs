using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core de la conversación entre ciudadano y el sistema.
/// </summary>
public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");

        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();

        b.Property(c => c.ExternalUserId)
            .HasColumnName("external_user_id")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(c => c.ExternalUserName)
            .HasColumnName("external_user_name")
            .HasMaxLength(200);

        b.Property(c => c.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        b.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TicketStatus.BotActive)
            .IsRequired();

        b.Property(c => c.IsAuthenticated)
            .HasColumnName("is_authenticated")
            .HasDefaultValue(false);

        b.Property(c => c.CustomerEmail)
            .HasColumnName("customer_email")
            .HasMaxLength(200);

        b.Property(c => c.CustomerPhone)
            .HasColumnName("customer_phone")
            .HasMaxLength(30);

        b.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.Property(c => c.LastMessageAt)
            .HasColumnName("last_message_at")
            .HasDefaultValueSql("now()");

        b.Property(c => c.ResolvedAt).HasColumnName("resolved_at");

        b.Property(c => c.FlowNodeId)
            .HasColumnName("flow_node_id")
            .HasMaxLength(80);

        b.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(c => c.Ticket)
            .WithOne(t => t.Conversation)
            .HasForeignKey<Ticket>(t => t.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(c => new { c.ExternalUserId, c.Channel })
            .HasDatabaseName("ix_conversations_external_user_channel");

        b.HasIndex(c => c.Status)
            .HasDatabaseName("ix_conversations_status");
    }
}
