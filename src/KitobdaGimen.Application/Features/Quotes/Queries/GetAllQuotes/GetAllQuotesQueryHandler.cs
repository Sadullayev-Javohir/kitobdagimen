using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetAllQuotes;

public class GetAllQuotesQueryHandler : IRequestHandler<GetAllQuotesQuery, PagedResult<QuoteDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAllQuotesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<QuoteDto>> Handle(GetAllQuotesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var currentUserId = _currentUser.UserId;
        var viewerEmail = _currentUser.Email?.ToLowerInvariant();
        var sort = (request.Sort ?? "latest").Trim().ToLowerInvariant();

        IQueryable<Quote> query = _db.Quotes.AsNoTracking();

        if (sort == "saved")
        {
            if (currentUserId.HasValue)
            {
                query = query.Where(q => q.SavedBy.Any(s => s.UserId == currentUserId.Value));
            }
            else
            {
                query = query.Where(_ => false);
            }
        }
        else if (sort == "mine")
        {
            if (currentUserId.HasValue)
            {
                query = query.Where(q => q.UserId == currentUserId.Value);
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(q =>
                q.Text.ToLower().Contains(term) ||
                q.Book.Title.ToLower().Contains(term) ||
                q.Book.Author.ToLower().Contains(term) ||
                q.User.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        IOrderedQueryable<Quote> ordered = sort switch
        {
            "popular" => query
                .OrderByDescending(q => q.Likes.Count)
                .ThenByDescending(q => q.SavedBy.Count)
                .ThenByDescending(q => q.Views.Count)
                .ThenByDescending(q => q.CreatedAt),
            _ => query.OrderByDescending(q => q.CreatedAt)
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToQuoteDto(currentUserId, viewerEmail)
            .ToListAsync(cancellationToken);

        return PagedResult<QuoteDto>.Create(items, page, pageSize, totalCount);
    }
}
