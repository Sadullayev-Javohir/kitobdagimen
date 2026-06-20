using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetSavedQuotes;

public class GetSavedQuotesQueryHandler : IRequestHandler<GetSavedQuotesQuery, PagedResult<QuoteDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSavedQuotesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<QuoteDto>> Handle(GetSavedQuotesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var saved = _db.SavedQuotes.Where(s => s.UserId == userId);

        var totalCount = await saved.CountAsync(cancellationToken);

        var items = await saved
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.Quote)
            .ToQuoteDto(userId)
            .ToListAsync(cancellationToken);

        return PagedResult<QuoteDto>.Create(items, page, pageSize, totalCount);
    }
}
