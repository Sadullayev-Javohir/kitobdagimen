using KitobdaGimen.Application.Features.Posts.Dtos;

namespace KitobdaGimen.Application.Features.Quotes.Dtos;

/// <summary>A single quote together with its full comment thread.</summary>
public record QuoteDetailDto
{
    public QuoteDto Quote { get; init; } = null!;
    public IReadOnlyList<CommentDto> Comments { get; init; } = Array.Empty<CommentDto>();
}
