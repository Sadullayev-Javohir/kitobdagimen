using KitobdaGimen.Application.Common.Feed;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuotes;

public class GetQuotesQueryHandler : IRequestHandler<GetQuotesQuery, PagedResult<QuoteDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetQuotesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<QuoteDto>> Handle(GetQuotesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var userId = _currentUser.UserId;

        // Filtering to one book or searching scans those quotes by recency — the follow blend doesn't apply.
        if (request.BookId is int bookId)
        {
            return await PageByRecencyAsync(_db.Quotes.Where(q => q.BookId == bookId), page, pageSize, userId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // ToLower().Contains keeps this provider-agnostic (no ILike in Application layer).
            var term = request.Search.Trim().ToLower();
            var matches = _db.Quotes.Where(q =>
                q.Text.ToLower().Contains(term) ||
                q.Book.Title.ToLower().Contains(term) ||
                q.Book.Author.ToLower().Contains(term) ||
                q.User.FullName.ToLower().Contains(term));

            return await PageByRecencyAsync(matches, page, pageSize, userId, cancellationToken);
        }

        var followingIds = userId is null
            ? new List<int>()
            : await _db.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync(cancellationToken);

        // Following no one (or anonymous) → plain global list: recent quotes from many users.
        if (followingIds.Count == 0)
        {
            return await PageByRecencyAsync(_db.Quotes, page, pageSize, userId, cancellationToken);
        }

        // Two recency-ordered buckets: followed authors (+ the user's own) and everyone else.
        var followed = _db.Quotes.Where(q => followingIds.Contains(q.UserId) || q.UserId == userId);
        var others = _db.Quotes.Where(q => !followingIds.Contains(q.UserId) && q.UserId != userId);

        var followedTotal = await followed.CountAsync(cancellationToken);
        var othersTotal = await others.CountAsync(cancellationToken);

        var plan = FollowMixPlan.Build(page, pageSize, followedTotal, othersTotal);

        var followedItems = plan.FollowedTake > 0
            ? await TakeByRecencyAsync(followed, plan.FollowedSkip, plan.FollowedTake, userId, cancellationToken)
            : new List<QuoteDto>();
        var otherItems = plan.NonFollowedTake > 0
            ? await TakeByRecencyAsync(others, plan.NonFollowedSkip, plan.NonFollowedTake, userId, cancellationToken)
            : new List<QuoteDto>();

        var items = Weave(plan.Order, followedItems, otherItems);
        return PagedResult<QuoteDto>.Create(items, page, pageSize, followedTotal + othersTotal);
    }

    private async Task<PagedResult<QuoteDto>> PageByRecencyAsync(
        IQueryable<Quote> source, int page, int pageSize, int? userId, CancellationToken ct)
    {
        var totalCount = await source.CountAsync(ct);
        var items = await TakeByRecencyAsync(source, (page - 1) * pageSize, pageSize, userId, ct);
        return PagedResult<QuoteDto>.Create(items, page, pageSize, totalCount);
    }

    private Task<List<QuoteDto>> TakeByRecencyAsync(
        IQueryable<Quote> source, int skip, int take, int? userId, CancellationToken ct)
        => source
            .OrderByDescending(q => q.CreatedAt)
            .ThenByDescending(q => q.Id)
            .Skip(skip)
            .Take(take)
            .ToQuoteDto(userId)
            .ToListAsync(ct);

    /// <summary>Interleaves the two buckets following the plan's cadence, falling back to
    /// whichever bucket still has items so the page is filled even when one runs short.</summary>
    private static List<QuoteDto> Weave(IReadOnlyList<bool> order, List<QuoteDto> followed, List<QuoteDto> others)
    {
        var items = new List<QuoteDto>(order.Count);
        int fi = 0, ni = 0;
        foreach (var pickFollowed in order)
        {
            if (pickFollowed && fi < followed.Count) items.Add(followed[fi++]);
            else if (!pickFollowed && ni < others.Count) items.Add(others[ni++]);
            else if (fi < followed.Count) items.Add(followed[fi++]);
            else if (ni < others.Count) items.Add(others[ni++]);
        }
        return items;
    }
}
