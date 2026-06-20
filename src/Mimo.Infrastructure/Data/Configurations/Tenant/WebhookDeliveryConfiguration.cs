using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core de los intentos de entrega de webhook (cola + bitácora).
/// </summary>
public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> b)
    {
        b.ToTable("webhook_deliveries");

        b.HasKey(d => d.Id);
        b.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(d => d.SubscriptionId).HasColumnName("subscription_id").IsRequired();
        b.Property(d => d.EventType).HasColumnName("event_type").HasMaxLength(80).IsRequired();
        b.Property(d => d.Payload).HasColumnName("payload").IsRequired();

        // Estado almacenado como entero (enum); por defecto Pending (0).
        b.Property(d => d.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(WebhookDeliveryStatus.Pending);

        b.Property(d => d.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        b.Property(d => d.NextAttemptAt).HasColumnName("next_attempt_at");
        b.Property(d => d.LastAttemptAt).HasColumnName("last_attempt_at");
        b.Property(d => d.ResponseStatusCode).HasColumnName("response_status_code");
        b.Property(d => d.LastError).HasColumnName("last_error").HasMaxLength(1000);
        b.Property(d => d.ClaimedBy).HasColumnName("claimed_by").HasMaxLength(64);
        b.Property(d => d.ClaimedAt).HasColumnName("claimed_at");
        b.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        b.HasOne(d => d.Subscription)
            .WithMany()
            .HasForeignKey(d => d.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // El worker busca entregas pendientes listas para enviar; índice por estado.
        b.HasIndex(d => d.Status).HasDatabaseName("ix_webhook_deliveries_status");
    }
}
