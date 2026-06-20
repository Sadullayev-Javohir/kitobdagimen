using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetUserQuotes;

/// <summary>
/// Returns quotes authored by a specific user, newest first, paged. Used on public
/// profiles (<c>/u/{username}</c>) so anyone — even anonymous visitors — can see a
/// user's quotes. Save state is computed for the current viewer (none when anonymous).
/// </summary>
public record GetUserQuotesQuery(int UserId) : IRequest<PagedResult<QuoteDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
