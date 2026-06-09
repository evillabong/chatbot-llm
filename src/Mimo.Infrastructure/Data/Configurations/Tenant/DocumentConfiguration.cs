using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Enums;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core del documento de conocimiento.
/// El embedding se mapea como vector(1536) para pgvector.
/// El índice HNSW se crea en la migración para búsqueda eficiente por coseno.
/// </summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");

        b.HasKey(d => d.Id);
        b.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(d => d.TenantId).HasColumnName("tenant_id").IsRequired();

        b.Property(d => d.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        b.Property(d => d.Content)
            .HasColumnName("content")
            .IsRequired();

        b.Property(d => d.Visibility)
            .HasColumnName("visibility")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(VisibilityLevel.Public)
            .IsRequired();

        b.Property(d => d.CategoryId).HasColumnName("category_id");
        b.Property(d => d.RelatedRoleId).HasColumnName("related_role_id");

        b.Property(d => d.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'");

        b.Property(d => d.PriorityLevel)
            .HasColumnName("priority_level")
            .HasDefaultValue(0);

        // Vector de embeddings para búsqueda semántica con pgvector (1536 dimensiones)
        b.Property(d => d.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("vector(1536)");

        b.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        b.Property(d => d.CreatedBy).HasColumnName("created_by");

        b.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        b.Property(d => d.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(d => d.TenantId).HasDatabaseName("ix_documents_tenant_id");
        b.HasIndex(d => d.IsActive).HasDatabaseName("ix_documents_is_active");
    }
}
