using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetMyQuotes;

/// <summary>Returns quotes authored by the current user, newest first, paged.</summary>
public record GetMyQuotesQuery : IRequest<PagedResult<QuoteDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
