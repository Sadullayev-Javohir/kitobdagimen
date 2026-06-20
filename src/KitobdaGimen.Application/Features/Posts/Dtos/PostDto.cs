using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.Posts.Dtos;

/// <summary>A post as shown in the feed, with engagement counters.</summary>
public record PostDto
{
    public int Id { get; init; }
    /// <summary>Short public identifier used in the shareable URL.</summary>
    public string Slug { get; init; } = null!;
    public string ReviewText { get; init; } = null!;
    public string? ImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }

    public UserSummaryDto Author { get; init; } = null!;
    public BookSummaryDto Book { get; init; } = null!;

    public int LikeCount { get; init; }
    public int CommentCount { get; init; }
    public int ViewCount { get; init; }
    public bool IsLikedByCurrentUser { get; init; }

    /// <summary>True when the current user is this post's author.</summary>
    public bool IsAuthor { get; init; }
    /// <summary>True when the current user already follows this post's author.</summary>
    public bool IsFollowingAuthor { get; init; }

    /// <summary>True when this post's author has at least one story (shows a tappable ring).</summary>
    public bool AuthorHasStory { get; init; }
}
