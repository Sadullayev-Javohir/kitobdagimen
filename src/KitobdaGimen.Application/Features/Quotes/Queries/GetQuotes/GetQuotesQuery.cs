using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuotes;

/// <summary>Returns quotes (optionally filtered to a single book), newest first, paged.</summary>
public record GetQuotesQuery : IRequest<PagedResult<QuoteDto>>
{
    public int? BookId { get; init; }

    /// <summary>Free-text search across quote text, book title, book author and the quote author's name.</summary>
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
