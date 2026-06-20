using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetSavedQuotes;

/// <summary>Returns quotes the current user has saved, most recently saved first, paged.</summary>
public record GetSavedQuotesQuery : IRequest<PagedResult<QuoteDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
