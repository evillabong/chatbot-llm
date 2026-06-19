using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core de las suscripciones de webhook saliente del tenant.
/// </summary>
public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> b)
    {
        b.ToTable("webhook_subscriptions");

        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(s => s.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        b.Property(s => s.Url).HasColumnName("url").HasMaxLength(2048).IsRequired();
        b.Property(s => s.SecretProtected).HasColumnName("secret_protected").IsRequired();

        // Lista de eventos como columna text[] de PostgreSQL.
        b.Property(s => s.Events).HasColumnName("events").HasColumnType("text[]").IsRequired();

        b.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(s => s.RevokedAt).HasColumnName("revoked_at");

        b.HasIndex(s => s.IsActive).HasDatabaseName("ix_webhook_subscriptions_active");
    }
}
