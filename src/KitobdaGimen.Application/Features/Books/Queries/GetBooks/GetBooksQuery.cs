using KitobdaGimen.Application.Features.Books.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Books.Queries.GetBooks;

/// <summary>Searches books by title or author (for pickers). Empty term returns recent books.</summary>
public record GetBooksQuery : IRequest<IReadOnlyList<BookDto>>
{
    public string? Search { get; init; }
    public int Limit { get; init; } = 20;
}
