using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId);
        builder.Property(x => x.OwnerUserId);
        builder.Property(x => x.CreatedByUserId);
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.OwnerUserId);
    }
}
