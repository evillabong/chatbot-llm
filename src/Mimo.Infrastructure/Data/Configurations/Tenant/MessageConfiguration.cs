using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core del mensaje individual dentro de una conversación.
/// </summary>
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");

        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(m => m.ConversationId).HasColumnName("conversation_id").IsRequired();

        b.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        b.Property(m => m.SenderId).HasColumnName("sender_id");

        b.Property(m => m.Content)
            .HasColumnName("content")
            .IsRequired();

        // Metadatos: documentos usados, intenciones detectadas, herramientas MCP invocadas
        b.Property(m => m.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        b.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .HasDatabaseName("ix_messages_conversation_created");
    }
}
