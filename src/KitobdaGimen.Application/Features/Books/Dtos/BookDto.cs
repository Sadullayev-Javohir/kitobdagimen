namespace KitobdaGimen.Application.Features.Books.Dtos;

public record BookDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string Author { get; init; } = null!;
    public string? CoverUrl { get; init; }
    public int TotalPages { get; init; }
    public int? GenreId { get; init; }
}
