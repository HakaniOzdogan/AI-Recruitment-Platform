using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class InterviewSessionConfiguration : IEntityTypeConfiguration<InterviewSession>
{
    public void Configure(EntityTypeBuilder<InterviewSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId);
        builder.Property(x => x.InterviewerUserId);
        builder.Property(x => x.ApplicantUserId);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.AiMode).HasMaxLength(20).IsRequired().HasDefaultValue("OFF");
        builder.Property(x => x.AiModelName).HasMaxLength(120);
        builder.Property(x => x.AiLastPlanAt);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CompletedAt);

        builder.HasOne(x => x.Application)
            .WithMany(x => x.InterviewSessions)
            .HasForeignKey(x => x.ApplicationId);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.InterviewerUserId);
        builder.HasIndex(x => x.ApplicantUserId);
    }
}
