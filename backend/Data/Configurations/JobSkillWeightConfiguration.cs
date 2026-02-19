using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class JobSkillWeightConfiguration : IEntityTypeConfiguration<JobSkillWeight>
{
    public void Configure(EntityTypeBuilder<JobSkillWeight> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SkillNameNormalized).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Weight).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Job)
            .WithMany(x => x.SkillWeights)
            .HasForeignKey(x => x.JobId);

        builder.HasIndex(x => new { x.JobId, x.SkillNameNormalized }).IsUnique();
    }
}
