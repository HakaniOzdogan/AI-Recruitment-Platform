using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(150);
        builder.Property(x => x.Location).HasMaxLength(150);
        builder.Property(x => x.EmploymentType).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.RequiredSkillsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.NiceToHaveSkillsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.CompetencyWeightsJson).HasColumnType("text");
        builder.Property(x => x.MinExperienceMonths);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
