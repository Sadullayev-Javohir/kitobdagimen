using KitobdaGimen.Application.Features.Books.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Books.Commands.CreateBook;

/// <summary>Adds a book to the catalogue (reused if an identical title+author already exists).</summary>
public record CreateBookCommand : IRequest<BookDto>
{
    public string Title { get; init; } = null!;
    public string Author { get; init; } = null!;
    public int TotalPages { get; init; }
    public string? CoverUrl { get; init; }
    public int? GenreId { get; init; }
    /// <summary>Tashqi manba nomi (masalan "asaxiy.uz"); qo'lda qo'shilganda null.</summary>
    public string? Source { get; init; }
}
