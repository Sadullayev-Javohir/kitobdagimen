using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetAllQuotes;

public record GetAllQuotesQuery : IRequest<PagedResult<QuoteDto>>
{
    public string? Search { get; init; }
    public string Sort { get; init; } = "latest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
