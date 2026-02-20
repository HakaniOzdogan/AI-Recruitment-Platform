using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.AppliedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.CreatedByUserId);
        builder.Property(x => x.LastUpdatedByUserId).IsRequired();

        builder.HasOne(x => x.Job)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.JobId);

        builder.HasOne(x => x.Candidate)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.CandidateId);

        builder.HasOne(x => x.Stage)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.StageId)
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.JobId, x.CandidateId }).IsUnique();
    }
}
