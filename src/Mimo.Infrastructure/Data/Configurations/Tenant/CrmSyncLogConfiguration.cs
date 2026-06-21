using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core de la bitácora de sincronización con CRM externo (#26).</summary>
public class CrmSyncLogConfiguration : IEntityTypeConfiguration<CrmSyncLog>
{
    public void Configure(EntityTypeBuilder<CrmSyncLog> b)
    {
        b.ToTable("crm_sync_logs");

        b.HasKey(l => l.Id);
        b.Property(l => l.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(l => l.OpportunityId).HasColumnName("opportunity_id").IsRequired();
        b.Property(l => l.Provider).HasColumnName("provider").HasMaxLength(60).IsRequired();
        b.Property(l => l.Success).HasColumnName("success").IsRequired();
        b.Property(l => l.ExternalId).HasColumnName("external_id").HasMaxLength(200);
        b.Property(l => l.Message).HasColumnName("message").HasMaxLength(500);
        b.Property(l => l.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        b.HasIndex(l => new { l.OpportunityId, l.CreatedAt }).HasDatabaseName("ix_crm_sync_logs_opportunity_created");
    }
}
