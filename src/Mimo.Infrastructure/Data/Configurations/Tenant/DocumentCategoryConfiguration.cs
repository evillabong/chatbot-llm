using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core de categorías de documentos con soporte jerárquico.
/// </summary>
public class DocumentCategoryConfiguration : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> b)
    {
        b.ToTable("document_categories");

        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();

        b.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(c => c.ParentCategoryId).HasColumnName("parent_category_id");

        b.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
