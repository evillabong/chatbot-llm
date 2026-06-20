using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core de los flujos guiados del chatbot por opciones (#27).
/// </summary>
public class ChatbotFlowConfiguration : IEntityTypeConfiguration<ChatbotFlow>
{
    public void Configure(EntityTypeBuilder<ChatbotFlow> b)
    {
        b.ToTable("chatbot_flows");

        b.HasKey(f => f.Id);
        b.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(f => f.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        b.Property(f => f.IsActive).HasColumnName("is_active").HasDefaultValue(false);
        b.Property(f => f.Definition).HasColumnName("definition").HasColumnType("jsonb").IsRequired();
        b.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(f => f.UpdatedAt).HasColumnName("updated_at");

        // Solo un flujo activo por tenant (índice único parcial sobre is_active = true).
        b.HasIndex(f => f.IsActive)
            .HasDatabaseName("ux_chatbot_flows_active")
            .IsUnique()
            .HasFilter("is_active");
    }
}
