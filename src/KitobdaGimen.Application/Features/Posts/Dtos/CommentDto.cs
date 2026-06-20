using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.Posts.Dtos;

/// <summary>A comment on a post, with its direct replies nested underneath.</summary>
public record CommentDto
{
    public int Id { get; init; }
    public string Text { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public UserSummaryDto Author { get; init; } = null!;
    public int? ParentCommentId { get; init; }
    public IReadOnlyList<CommentDto> Replies { get; init; } = Array.Empty<CommentDto>();

    /// <summary>True when this comment's author is also the post's author.</summary>
    public bool IsPostAuthor { get; init; }
}
