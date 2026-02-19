using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IkOtomasyon.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Method).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Path).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(600).IsRequired();
        builder.Property(x => x.Entity).HasMaxLength(200);
        builder.Property(x => x.EntityId).HasMaxLength(200);
        builder.Property(x => x.StatusCode).IsRequired();
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(400);
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
