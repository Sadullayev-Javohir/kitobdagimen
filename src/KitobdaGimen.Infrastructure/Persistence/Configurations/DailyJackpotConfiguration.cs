using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class DailyJackpotConfiguration : IEntityTypeConfiguration<DailyJackpot>
{
    public void Configure(EntityTypeBuilder<DailyJackpot> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Type)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(j => j.Date)
            .IsUnique();

        builder.HasOne(j => j.User)
            .WithMany()
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Post)
            .WithMany()
            .HasForeignKey(j => j.PostId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.Quote)
            .WithMany()
            .HasForeignKey(j => j.QuoteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
