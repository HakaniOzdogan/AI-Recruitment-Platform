using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class InterviewQuestionBankConfiguration : IEntityTypeConfiguration<InterviewQuestionBank>
{
    public void Configure(EntityTypeBuilder<InterviewQuestionBank> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(30).IsRequired();
        builder.Property(x => x.TopicKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Difficulty).IsRequired();
        builder.Property(x => x.QuestionText).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TagsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.Category, x.TopicKey, x.IsActive });
    }
}
