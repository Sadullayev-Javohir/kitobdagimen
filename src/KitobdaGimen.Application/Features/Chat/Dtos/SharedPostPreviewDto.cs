namespace KitobdaGimen.Application.Features.Chat.Dtos;

/// <summary>Lightweight preview of a post shared inside a chat message.</summary>
public record SharedPostPreviewDto
{
    public int PostId { get; init; }
    public string BookTitle { get; init; } = null!;
    public string BookAuthor { get; init; } = null!;
    public string AuthorName { get; init; } = null!;
}
