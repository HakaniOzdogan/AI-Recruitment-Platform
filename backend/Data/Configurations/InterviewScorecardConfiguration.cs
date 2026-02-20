using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class InterviewScorecardConfiguration : IEntityTypeConfiguration<InterviewScorecard>
{
    public void Configure(EntityTypeBuilder<InterviewScorecard> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId);
        builder.Property(x => x.OverallScore);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Scorecards)
            .HasForeignKey(x => x.SessionId);

        builder.HasOne(x => x.RubricTemplate)
            .WithMany(x => x.Scorecards)
            .HasForeignKey(x => x.RubricTemplateId);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.SessionId, x.RubricTemplateId }).IsUnique();
    }
}
