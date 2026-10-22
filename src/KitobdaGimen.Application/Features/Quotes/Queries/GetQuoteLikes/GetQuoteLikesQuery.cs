using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteLikes;

public record GetQuoteLikesQuery : IRequest<PagedResult<QuoteLikerDto>>
{
    public int QuoteId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
