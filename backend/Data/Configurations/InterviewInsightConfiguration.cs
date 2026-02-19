using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class InterviewInsightConfiguration : IEntityTypeConfiguration<InterviewInsight>
{
    public void Configure(EntityTypeBuilder<InterviewInsight> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TurnIndex).IsRequired();
        builder.Property(x => x.SignalsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.CompetencyJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.DepthJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.RiskFlagsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.EvidenceSnippetsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Insights)
            .HasForeignKey(x => x.SessionId);

        builder.HasIndex(x => new { x.SessionId, x.TurnIndex });
    }
}
