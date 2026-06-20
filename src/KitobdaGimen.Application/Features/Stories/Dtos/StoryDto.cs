using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.Stories.Dtos;

/// <summary>A story as shown in the viewer: a title, text, optional image, and engagement.</summary>
public record StoryDto
{
    public int Id { get; init; }

    public string Title { get; init; } = null!;
    public string Text { get; init; } = null!;

    /// <summary>Optional uploaded image shown above the text.</summary>
    public string? ImageUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public UserSummaryDto Author { get; init; } = null!;

    public int ViewCount { get; init; }
    public int LikeCount { get; init; }
    public bool IsLikedByCurrentUser { get; init; }

    /// <summary>True when this story belongs to the current user.</summary>
    public bool IsMine { get; init; }
}
