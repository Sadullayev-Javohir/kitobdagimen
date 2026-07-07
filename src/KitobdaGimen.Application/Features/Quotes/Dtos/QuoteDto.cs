using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.Quotes.Dtos;

/// <summary>A book quote with its author, source book and save/like state.</summary>
public record QuoteDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = null!;
    public string Text { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public UserSummaryDto Author { get; init; } = null!;
    public BookSummaryDto Book { get; init; } = null!;
    public int SaveCount { get; init; }
    public bool IsSavedByCurrentUser { get; init; }

    public int LikeCount { get; init; }
    public int CommentCount { get; init; }
    public int ViewCount { get; init; }
    public bool IsLikedByCurrentUser { get; init; }

    /// <summary>True when the current user is this quote's author.</summary>
    public bool IsAuthor { get; init; }
    /// <summary>True when the current user already follows this quote's author.</summary>
    public bool IsFollowingAuthor { get; init; }
    /// <summary>True when this quote's author has at least one live story (shows a tappable ring).</summary>
    public bool AuthorHasStory { get; init; }
}
