using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>Configuración EF Core de las oportunidades de venta (#26).</summary>
public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> b)
    {
        b.ToTable("opportunities");

        b.HasKey(o => o.Id);
        b.Property(o => o.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(o => o.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(o => o.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        b.Property(o => o.ContactName).HasColumnName("contact_name").HasMaxLength(160);
        b.Property(o => o.ContactEmail).HasColumnName("contact_email").HasMaxLength(160);
        b.Property(o => o.ContactPhone).HasColumnName("contact_phone").HasMaxLength(40);
        b.Property(o => o.Stage).HasColumnName("stage").HasConversion<int>().IsRequired();
        b.Property(o => o.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)").HasDefaultValue(0m);
        b.Property(o => o.ConversationId).HasColumnName("conversation_id");
        b.Property(o => o.AssignedAgentId).HasColumnName("assigned_agent_id");
        b.Property(o => o.Notes).HasColumnName("notes");
        b.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        b.Property(o => o.ClosedAt).HasColumnName("closed_at");
        b.Property(o => o.ExternalCrmId).HasColumnName("external_crm_id").HasMaxLength(200);
        b.Property(o => o.LastSyncedAt).HasColumnName("last_synced_at");

        // Pipeline por etapa y por responsable, más recientes primero.
        b.HasIndex(o => new { o.Stage, o.CreatedAt }).HasDatabaseName("ix_opportunities_stage_created");
        b.HasIndex(o => o.AssignedAgentId).HasDatabaseName("ix_opportunities_assigned_agent");
    }
}
