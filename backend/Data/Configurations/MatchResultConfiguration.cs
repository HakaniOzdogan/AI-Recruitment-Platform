using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class MatchResultConfiguration : IEntityTypeConfiguration<MatchResult>
{
    public void Configure(EntityTypeBuilder<MatchResult> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Score).IsRequired();
        builder.Property(x => x.ReasonsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.GapsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.ComputedAt).IsRequired();

        builder.HasOne(x => x.Job)
            .WithMany(x => x.MatchResults)
            .HasForeignKey(x => x.JobId);

        builder.HasOne(x => x.Candidate)
            .WithMany()
            .HasForeignKey(x => x.CandidateId);

        builder.HasIndex(x => new { x.JobId, x.CandidateId }).IsUnique();
    }
}
