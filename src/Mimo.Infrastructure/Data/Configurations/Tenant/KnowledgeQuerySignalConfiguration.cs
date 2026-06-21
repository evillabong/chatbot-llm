using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core de las señales de recuperación semántica (#22, Fase 1).</summary>
public class KnowledgeQuerySignalConfiguration : IEntityTypeConfiguration<KnowledgeQuerySignal>
{
    public void Configure(EntityTypeBuilder<KnowledgeQuerySignal> b)
    {
        b.ToTable("knowledge_query_signals");

        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(s => s.ConversationId).HasColumnName("conversation_id").IsRequired();
        b.Property(s => s.MessageId).HasColumnName("message_id");
        b.Property(s => s.QueryText).HasColumnName("query_text").HasMaxLength(2000).IsRequired();
        b.Property(s => s.TopSimilarity).HasColumnName("top_similarity");
        b.Property(s => s.MatchCount).HasColumnName("match_count").IsRequired();
        b.Property(s => s.KnowledgeGap).HasColumnName("knowledge_gap").IsRequired();
        b.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        // Listado de vacíos recientes en la UI de curación (Fase 1).
        b.HasIndex(s => new { s.KnowledgeGap, s.CreatedAt }).HasDatabaseName("ix_knowledge_signals_gap_created");
        b.HasIndex(s => s.ConversationId).HasDatabaseName("ix_knowledge_signals_conversation");
    }
}
