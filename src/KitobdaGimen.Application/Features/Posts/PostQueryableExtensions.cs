using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Features.Posts;

internal static class PostQueryableExtensions
{
    /// <summary>
    /// Projects posts into <see cref="PostDto"/>, computing engagement counts and whether the
    /// given user liked each post — all translated to SQL by EF Core.
    /// </summary>
    public static IQueryable<PostDto> ToPostDto(this IQueryable<Post> query, int? currentUserId)
    {
        return query.Select(p => new PostDto
        {
            Id = p.Id,
            Slug = p.Slug,
            ReviewText = p.ReviewText,
            ImageUrl = p.ImageUrl,
            CreatedAt = p.CreatedAt,
            Author = new UserSummaryDto
            {
                Id = p.User.Id,
                Username = p.User.Username,
                FullName = p.User.FullName,
                AvatarUrl = p.User.AvatarUrl
            },
            Book = new BookSummaryDto
            {
                Id = p.Book.Id,
                Title = p.Book.Title,
                Author = p.Book.Author,
                CoverUrl = p.Book.CoverUrl,
                GenreName = p.Book.Genre != null ? p.Book.Genre.Name : null
            },
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            ViewCount = p.Views.Count,
            IsLikedByCurrentUser = currentUserId != null && p.Likes.Any(l => l.UserId == currentUserId),
            IsAuthor = currentUserId != null && p.UserId == currentUserId,
            IsFollowingAuthor = currentUserId != null && p.User.Followers.Any(f => f.FollowerId == currentUserId),
            AuthorHasStory = p.User.Stories.Any(s => s.ExpiresAt > DateTime.UtcNow)
        });
    }
}
