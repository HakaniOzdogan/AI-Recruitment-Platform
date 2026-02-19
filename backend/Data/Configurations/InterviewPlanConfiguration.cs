using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class InterviewPlanConfiguration : IEntityTypeConfiguration<InterviewPlan>
{
    public void Configure(EntityTypeBuilder<InterviewPlan> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromTurnIndex).IsRequired();
        builder.Property(x => x.PlannedQuestionsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.PlanRationaleJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Plans)
            .HasForeignKey(x => x.SessionId);

        builder.HasIndex(x => new { x.SessionId, x.FromTurnIndex });
    }
}
