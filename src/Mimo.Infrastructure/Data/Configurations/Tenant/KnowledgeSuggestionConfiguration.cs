using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core de las sugerencias de conocimiento (#22, Fase 1 — curación).</summary>
public class KnowledgeSuggestionConfiguration : IEntityTypeConfiguration<KnowledgeSuggestion>
{
    public void Configure(EntityTypeBuilder<KnowledgeSuggestion> b)
    {
        b.ToTable("knowledge_suggestions");

        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(s => s.SourceSignalId).HasColumnName("source_signal_id");
        b.Property(s => s.DraftTitle).HasColumnName("draft_title").HasMaxLength(200).IsRequired();
        b.Property(s => s.DraftContent).HasColumnName("draft_content").IsRequired();
        b.Property(s => s.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        b.Property(s => s.PublishedDocumentId).HasColumnName("published_document_id");
        b.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(s => s.ReviewedAt).HasColumnName("reviewed_at");

        // Listado de la bandeja de curación por estado, más recientes primero.
        b.HasIndex(s => new { s.Status, s.CreatedAt }).HasDatabaseName("ix_knowledge_suggestions_status_created");
    }
}
