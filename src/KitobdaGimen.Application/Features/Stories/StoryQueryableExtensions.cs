using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Stories.Dtos;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Features.Stories;

internal static class StoryQueryableExtensions
{
    /// <summary>
    /// Keeps only stories that have not yet expired. Used everywhere stories are surfaced so that
    /// once a story's duration (12/24/48h) elapses it disappears and the profile looks plain again.
    /// </summary>
    public static IQueryable<Story> WhereActive(this IQueryable<Story> query)
        => query.Where(s => s.ExpiresAt > DateTime.UtcNow);

    /// <summary>
    /// Projects stories into <see cref="StoryDto"/>, computing engagement counts and whether the
    /// given user liked each story — all translated to SQL by EF Core.
    /// </summary>
    public static IQueryable<StoryDto> ToStoryDto(
        this IQueryable<Story> query, int? currentUserId, string? viewerEmail = null)
    {
        return query.Select(s => new StoryDto
        {
            Id = s.Id,
            Title = s.Title,
            Text = s.Text,
            ImageUrl = s.ImageUrl,
            CreatedAt = s.CreatedAt,
            Author = new UserSummaryDto
            {
                Id = s.User.Id,
                Username = s.User.Username,
                FullName = s.User.FullName,
                AvatarUrl = (s.User.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                             && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                             && viewerEmail != AvatarPrivacy.RestrictedEmail)
                    ? null
                    : s.User.AvatarUrl
            },
            ViewCount = s.Views.Count,
            LikeCount = s.Likes.Count,
            IsLikedByCurrentUser = currentUserId != null && s.Likes.Any(l => l.UserId == currentUserId),
            IsMine = currentUserId != null && s.UserId == currentUserId
        });
    }
}
