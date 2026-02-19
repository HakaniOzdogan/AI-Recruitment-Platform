using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class CandidateProfileConfiguration : IEntityTypeConfiguration<CandidateProfile>
{
    public void Configure(EntityTypeBuilder<CandidateProfile> builder)
    {
        builder.HasKey(x => x.CandidateId);
        builder.Property(x => x.FullNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EmailSnapshot).HasMaxLength(320);
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.SkillsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.EducationJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.ExperienceJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.LanguagesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.LinksJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Candidate)
            .WithOne(x => x.Profile)
            .HasForeignKey<CandidateProfile>(x => x.CandidateId);
    }
}
