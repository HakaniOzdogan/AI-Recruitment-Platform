using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class RubricTemplateConfiguration : IEntityTypeConfiguration<RubricTemplate>
{
    public void Configure(EntityTypeBuilder<RubricTemplate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Job)
            .WithMany(x => x.RubricTemplates)
            .HasForeignKey(x => x.JobId)
            .IsRequired(false);

        builder.HasIndex(x => new { x.JobId, x.IsDefault });
    }
}
