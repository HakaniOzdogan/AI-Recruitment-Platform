using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class InterviewCriterionScoreConfiguration : IEntityTypeConfiguration<InterviewCriterionScore>
{
    public void Configure(EntityTypeBuilder<InterviewCriterionScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CriterionKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired().HasDefaultValue(InterviewCriterionScoreStatus.Scored);
        builder.Property(x => x.Score);
        builder.Property(x => x.Rationale).HasColumnType("text");
        builder.Property(x => x.EvidenceQuotesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.EvaluatorType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.EnrichmentStatus).HasMaxLength(20).IsRequired().HasDefaultValue(InterviewScoreEnrichmentStatus.None);
        builder.Property(x => x.EnrichedByModel).HasMaxLength(120);
        builder.Property(x => x.EnrichmentErrorsJson).HasColumnType("text");
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Scorecard)
            .WithMany(x => x.CriterionScores)
            .HasForeignKey(x => x.ScorecardId);

        builder.HasOne(x => x.ReplacesScore)
            .WithMany()
            .HasForeignKey(x => x.ReplacesScoreId)
            .IsRequired(false);

        builder.HasIndex(x => new { x.ScorecardId, x.CriterionKey, x.CreatedAt });
    }
}
