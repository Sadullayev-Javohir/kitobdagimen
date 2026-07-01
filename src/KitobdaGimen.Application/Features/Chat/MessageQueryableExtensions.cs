using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat;

internal static class MessageQueryableExtensions
{
    /// <summary>Projects messages to <see cref="MessageDto"/>, including any shared-post preview.
    /// <paramref name="viewerEmail"/> — cheklangan foydalanuvchi avatarini yashirish uchun.</summary>
    public static IQueryable<MessageDto> ToMessageDto(
        this IQueryable<Message> query, int currentUserId, string? viewerEmail = null)
    {
        return query.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Sender = new UserSummaryDto
            {
                Id = m.Sender.Id,
                FullName = m.Sender.FullName,
                AvatarUrl = (m.Sender.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                             && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                             && viewerEmail != AvatarPrivacy.RestrictedEmail)
                    ? null
                    : m.Sender.AvatarUrl
            },
            Text = m.Text,
            ImageUrl = m.ImageUrl,
            StickerKey = m.StickerKey,
            SharedPost = m.SharedPost == null
                ? null
                : new SharedPostPreviewDto
                {
                    PostId = m.SharedPost.Id,
                    BookTitle = m.SharedPost.Book.Title,
                    BookAuthor = m.SharedPost.Book.Author,
                    AuthorName = m.SharedPost.User.FullName
                },
            SentAt = m.SentAt,
            IsRead = m.IsRead,
            EditedAt = m.EditedAt,
            IsMine = m.SenderId == currentUserId
        });
    }

    /// <summary>
    /// Loads emoji reactions for the given messages in a single query and fills each
    /// <see cref="MessageDto.Reactions"/> grouped by emoji. Grouping is done in memory so it
    /// works identically on Npgsql and the in-memory test provider.
    /// </summary>
    public static async Task AttachReactionsAsync(
        this IAppDbContext db, IReadOnlyCollection<MessageDto> messages, int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0) return;

        var ids = messages.Select(m => m.Id).ToList();
        var rows = await db.MessageReactions
            .Where(r => ids.Contains(r.MessageId))
            .Select(r => new { r.MessageId, r.Emoji, r.UserId })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return;

        var byMessage = rows
            .GroupBy(r => r.MessageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var message in messages)
        {
            if (!byMessage.TryGetValue(message.Id, out var reactions)) continue;
            message.Reactions = reactions
                .GroupBy(r => r.Emoji)
                .Select(g => new MessageReactionGroupDto
                {
                    Emoji = g.Key,
                    Count = g.Count(),
                    Mine = g.Any(x => x.UserId == currentUserId)
                })
                .OrderByDescending(g => g.Count)
                .ToList();
        }
    }
}
