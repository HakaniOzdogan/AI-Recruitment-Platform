using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class AiEvaluationReportConfiguration : IEntityTypeConfiguration<AiEvaluationReport>
{
    public void Configure(EntityTypeBuilder<AiEvaluationReport> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId);
        builder.Property(x => x.ModelName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InputSnapshotJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.InputSnapshotHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OverallRecommendation).HasMaxLength(20).IsRequired();
        builder.Property(x => x.StrengthsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.RisksJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.VerificationQuestionsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.EvidenceQuotesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.CompetencyAssessmentJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.SkillAssessmentJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.Confidence).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.Version).IsRequired();

        builder.HasOne(x => x.Job)
            .WithMany(x => x.AiEvaluationReports)
            .HasForeignKey(x => x.JobId);

        builder.HasOne(x => x.Candidate)
            .WithMany(x => x.AiEvaluationReports)
            .HasForeignKey(x => x.CandidateId);

        builder.HasOne(x => x.Application)
            .WithMany(x => x.AiEvaluationReports)
            .HasForeignKey(x => x.ApplicationId)
            .IsRequired(false);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.JobId, x.CandidateId, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.JobId, x.CandidateId, x.InputSnapshotHash });
    }
}
