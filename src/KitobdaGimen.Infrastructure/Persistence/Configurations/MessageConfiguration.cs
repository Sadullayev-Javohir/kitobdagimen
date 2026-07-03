using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Text).HasMaxLength(4000);
        builder.Property(m => m.ImageUrl).HasMaxLength(512);
        builder.Property(m => m.StickerKey).HasMaxLength(64);
        builder.Property(m => m.VoiceUrl).HasMaxLength(512);

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.SharedPost)
            .WithMany()
            .HasForeignKey(m => m.SharedPostId)
            .OnDelete(DeleteBehavior.SetNull);

        // Self-referencing "reply to" (Telegram-style quote). If the quoted message is deleted,
        // the reply survives with its foreign key nulled out.
        builder.HasOne(m => m.ReplyToMessage)
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.ConversationId, m.SentAt });
    }
}
