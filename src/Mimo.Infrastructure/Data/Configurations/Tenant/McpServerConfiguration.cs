using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core del catálogo de servidores MCP externos (#23).</summary>
public class McpServerConfiguration : IEntityTypeConfiguration<McpServer>
{
    public void Configure(EntityTypeBuilder<McpServer> b)
    {
        b.ToTable("mcp_servers");

        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(s => s.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        b.Property(s => s.Endpoint).HasColumnName("endpoint").HasMaxLength(2048).IsRequired();
        b.Property(s => s.AuthToken).HasColumnName("auth_token");
        b.Property(s => s.AllowedTools)
            .HasColumnName("allowed_tools")
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'");
        b.Property(s => s.IsEnabled).HasColumnName("is_enabled").IsRequired();
        b.Property(s => s.TimeoutSeconds).HasColumnName("timeout_seconds").IsRequired();
        b.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(s => s.Name).HasDatabaseName("ux_mcp_servers_name").IsUnique();
    }
}
