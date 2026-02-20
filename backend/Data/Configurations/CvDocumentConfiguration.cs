using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class CvDocumentConfiguration : IEntityTypeConfiguration<CvDocument>
{
    public void Configure(EntityTypeBuilder<CvDocument> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId);
        builder.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.FileType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FileSize).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.UploadedByUserId).IsRequired();
        builder.Property(x => x.ParseStatus).IsRequired();
        builder.Property(x => x.ParseError).HasMaxLength(1000);

        builder.HasOne(x => x.Candidate)
            .WithMany(x => x.CvDocuments)
            .HasForeignKey(x => x.CandidateId);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CandidateId);
        builder.HasIndex(x => x.UploadedAt);
    }
}
