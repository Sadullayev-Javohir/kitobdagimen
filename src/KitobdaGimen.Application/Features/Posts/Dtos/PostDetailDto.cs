namespace KitobdaGimen.Application.Features.Posts.Dtos;

/// <summary>A single post together with its full comment thread.</summary>
public record PostDetailDto
{
    public PostDto Post { get; init; } = null!;
    public IReadOnlyList<CommentDto> Comments { get; init; } = Array.Empty<CommentDto>();
}
