using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class InterviewMessageConfiguration : IEntityTypeConfiguration<InterviewMessage>
{
    public void Configure(EntityTypeBuilder<InterviewMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.SessionId);

        builder.HasOne(x => x.QuestionBank)
            .WithMany()
            .HasForeignKey(x => x.QuestionBankId)
            .IsRequired(false);

        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });
    }
}
