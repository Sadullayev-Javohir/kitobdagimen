using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Features.Quotes;

internal static class QuoteQueryableExtensions
{
    /// <summary>Projects quotes to <see cref="QuoteDto"/>, computing save count and save state.
    /// <paramref name="viewerEmail"/> — cheklangan foydalanuvchi avatarini yashirish uchun.</summary>
    public static IQueryable<QuoteDto> ToQuoteDto(
        this IQueryable<Quote> query, int? currentUserId, string? viewerEmail = null)
    {
        return query.Select(q => new QuoteDto
        {
            Id = q.Id,
            Text = q.Text,
            CreatedAt = q.CreatedAt,
            Author = new UserSummaryDto
            {
                Id = q.User.Id,
                Username = q.User.Username,
                FullName = q.User.FullName,
                AvatarUrl = (q.User.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                             && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                             && viewerEmail != AvatarPrivacy.RestrictedEmail)
                    ? null
                    : q.User.AvatarUrl
            },
            Book = new BookSummaryDto
            {
                Id = q.Book.Id,
                Title = q.Book.Title,
                Author = q.Book.Author,
                CoverUrl = q.Book.CoverUrl,
                Source = q.Book.Source
            },
            SaveCount = q.SavedBy.Count,
            IsSavedByCurrentUser = currentUserId != null && q.SavedBy.Any(s => s.UserId == currentUserId),
            LikeCount = q.Likes.Count,
            CommentCount = q.Comments.Count,
            IsLikedByCurrentUser = currentUserId != null && q.Likes.Any(l => l.UserId == currentUserId),
            IsAuthor = currentUserId != null && q.UserId == currentUserId,
            IsFollowingAuthor = currentUserId != null && q.User.Followers.Any(f => f.FollowerId == currentUserId),
            AuthorHasStory = q.User.Stories.Any(s => s.ExpiresAt > DateTime.UtcNow)
        });
    }
}
