using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class RubricCriterionConfiguration : IEntityTypeConfiguration<RubricCriterion>
{
    public void Configure(EntityTypeBuilder<RubricCriterion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Weight).IsRequired();
        builder.Property(x => x.Order).IsRequired();

        builder.HasOne(x => x.Template)
            .WithMany(x => x.Criteria)
            .HasForeignKey(x => x.TemplateId);

        builder.HasIndex(x => new { x.TemplateId, x.Key }).IsUnique();
        builder.HasIndex(x => new { x.TemplateId, x.Order });
    }
}
