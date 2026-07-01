using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetMyQuotes;

public class GetMyQuotesQueryHandler : IRequestHandler<GetMyQuotesQuery, PagedResult<QuoteDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyQuotesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<QuoteDto>> Handle(GetMyQuotesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var source = _db.Quotes.Where(q => q.UserId == userId);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToQuoteDto(userId, _currentUser.Email?.ToLowerInvariant())
            .ToListAsync(cancellationToken);

        return PagedResult<QuoteDto>.Create(items, page, pageSize, totalCount);
    }
}
