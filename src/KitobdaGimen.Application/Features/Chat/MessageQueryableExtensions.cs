using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;
using KitobdaGimen.Domain.Entities;

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
}
