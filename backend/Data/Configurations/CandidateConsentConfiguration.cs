using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class CandidateConsentConfiguration : IEntityTypeConfiguration<CandidateConsent>
{
    public void Configure(EntityTypeBuilder<CandidateConsent> builder)
    {
        builder.HasKey(x => x.CandidateId);
        builder.Property(x => x.ConsentGiven).IsRequired();
        builder.Property(x => x.ConsentTextVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ConsentAt).IsRequired();
        builder.Property(x => x.DataRetentionDays).IsRequired();

        builder.HasOne(x => x.Candidate)
            .WithOne(x => x.Consent)
            .HasForeignKey<CandidateConsent>(x => x.CandidateId);
    }
}
