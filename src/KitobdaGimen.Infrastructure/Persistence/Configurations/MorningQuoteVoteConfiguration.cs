using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class MorningQuoteVoteConfiguration : IEntityTypeConfiguration<MorningQuoteVote>
{
    public void Configure(EntityTypeBuilder<MorningQuoteVote> builder)
    {
        builder.ToTable("MorningQuoteVotes");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.ChatId).IsRequired();
        builder.Property(v => v.MessageId).IsRequired();
        builder.Property(v => v.TelegramUserId).IsRequired();
        builder.Property(v => v.IsPositive).IsRequired();
        builder.Property(v => v.VotedAt).IsRequired();

        builder.HasIndex(v => new { v.ChatId, v.MessageId, v.TelegramUserId }).IsUnique();
        builder.HasIndex(v => new { v.ChatId, v.MessageId, v.IsPositive });
    }
}

