using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mimo.Core.Models;

namespace Mimo.Infrastructure.Data.Configurations.Tenant;

/// <summary>
/// Configuración EF Core de la encuesta de satisfacción post-atención.
/// </summary>
public class SatisfactionSurveyConfiguration : IEntityTypeConfiguration<SatisfactionSurvey>
{
    public void Configure(EntityTypeBuilder<SatisfactionSurvey> b)
    {
        b.ToTable("satisfaction_surveys");

        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        b.Property(s => s.TicketId).HasColumnName("ticket_id").IsRequired();

        b.Property(s => s.Rating)
            .HasColumnName("rating")
            .IsRequired();

        // Restricción: calificación entre 1 y 5
        b.ToTable(t => t.HasCheckConstraint("ck_survey_rating", "rating BETWEEN 1 AND 5"));

        b.Property(s => s.Observations).HasColumnName("observations");

        b.Property(s => s.RecordedAt)
            .HasColumnName("recorded_at")
            .HasDefaultValueSql("now()");
    }
}
