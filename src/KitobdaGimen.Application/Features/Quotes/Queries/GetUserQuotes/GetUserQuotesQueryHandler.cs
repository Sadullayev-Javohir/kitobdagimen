using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetUserQuotes;

public class GetUserQuotesQueryHandler : IRequestHandler<GetUserQuotesQuery, PagedResult<QuoteDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUserQuotesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<QuoteDto>> Handle(GetUserQuotesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var source = _db.Quotes.Where(q => q.UserId == request.UserId);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToQuoteDto(_currentUser.UserId)
            .ToListAsync(cancellationToken);

        return PagedResult<QuoteDto>.Create(items, page, pageSize, totalCount);
    }
}
