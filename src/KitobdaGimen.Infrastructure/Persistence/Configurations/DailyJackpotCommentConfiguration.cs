using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class DailyJackpotCommentConfiguration : IEntityTypeConfiguration<DailyJackpotComment>
{
    public void Configure(EntityTypeBuilder<DailyJackpotComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(c => c.DailyJackpot)
            .WithMany(j => j.Comments)
            .HasForeignKey(c => c.DailyJackpotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
