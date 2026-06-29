using KitobdaGimen.Application.Common.Feed;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Application.Features.Quotes;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetFeed;

/// <summary>
/// Builds the unified feed: posts and quotes blended into one recency-ordered, paginated stream.
/// Keeps the original follow-mix cadence (followed authors + the user's own content lead, with a
/// share of non-followed content for discovery; global when the user follows no one). Quotes are
/// woven into each bucket by recency alongside posts. Search mode scans all posts (quotes excluded).
/// </summary>
public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, PagedResult<FeedItemDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFeedQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<FeedItemDto>> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var userId = _currentUser.UserId;
        var hasSearch = !string.IsNullOrWhiteSpace(request.Search);

        // ── Search: posts only, by recency (quotes are not searchable) ──
        if (hasSearch)
        {
            var term = request.Search!.Trim().ToLower();
            var matches = _db.Posts.Where(p =>
                p.ReviewText.ToLower().Contains(term) ||
                p.Book.Title.ToLower().Contains(term) ||
                p.Book.Author.ToLower().Contains(term) ||
                p.User.FullName.ToLower().Contains(term));

            var total = await matches.CountAsync(cancellationToken);
            var bucket = await TakeBucketAsync(matches, _db.Quotes.Where(_ => false), (page - 1) * pageSize, pageSize, userId, cancellationToken);
            return PagedResult<FeedItemDto>.Create(bucket, page, pageSize, total);
        }

        var followingIds = userId is null
            ? new List<int>()
            : await _db.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync(cancellationToken);

        // Following no one (or anonymous) → single global bucket: recent posts + quotes from everyone.
        if (followingIds.Count == 0)
        {
            var postTotalG = await _db.Posts.CountAsync(cancellationToken);
            var quoteTotalG = await _db.Quotes.CountAsync(cancellationToken);
            var itemsG = await TakeBucketAsync(_db.Posts, _db.Quotes, (page - 1) * pageSize, pageSize, userId, cancellationToken);
            return PagedResult<FeedItemDto>.Create(itemsG, page, pageSize, postTotalG + quoteTotalG);
        }

        // Two recency-ordered buckets: followed authors (+ the user's own) and everyone else.
        var followedPosts = _db.Posts.Where(p => followingIds.Contains(p.UserId) || p.UserId == userId);
        var otherPosts = _db.Posts.Where(p => !followingIds.Contains(p.UserId) && p.UserId != userId);
        var followedQuotes = _db.Quotes.Where(q => followingIds.Contains(q.UserId) || q.UserId == userId);
        var otherQuotes = _db.Quotes.Where(q => !followingIds.Contains(q.UserId) && q.UserId != userId);

        var followedTotal = await followedPosts.CountAsync(cancellationToken) + await followedQuotes.CountAsync(cancellationToken);
        var othersTotal = await otherPosts.CountAsync(cancellationToken) + await otherQuotes.CountAsync(cancellationToken);

        var plan = FollowMixPlan.Build(page, pageSize, followedTotal, othersTotal);

        var followedItems = plan.FollowedTake > 0
            ? await TakeBucketAsync(followedPosts, followedQuotes, plan.FollowedSkip, plan.FollowedTake, userId, cancellationToken)
            : new List<FeedItemDto>();
        var otherItems = plan.NonFollowedTake > 0
            ? await TakeBucketAsync(otherPosts, otherQuotes, plan.NonFollowedSkip, plan.NonFollowedTake, userId, cancellationToken)
            : new List<FeedItemDto>();

        var items = Weave(plan.Order, followedItems, otherItems);
        return PagedResult<FeedItemDto>.Create(items, page, pageSize, followedTotal + othersTotal);
    }

    /// <summary>
    /// Returns the <paramref name="skip"/>..<paramref name="take"/> slice of a bucket whose items are
    /// posts and quotes merged by recency. Each source is over-fetched to (skip+take) so the merged
    /// slice is exact.
    /// </summary>
    private static async Task<List<FeedItemDto>> TakeBucketAsync(
        IQueryable<Post> posts, IQueryable<Quote> quotes, int skip, int take, int? userId, CancellationToken ct)
    {
        var need = skip + take;
        if (need <= 0)
        {
            return new List<FeedItemDto>();
        }

        var postItems = await posts
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Take(need)
            .ToPostDto(userId)
            .ToListAsync(ct);

        var quoteItems = await quotes
            .OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.Id)
            .Take(need)
            .ToQuoteDto(userId)
            .ToListAsync(ct);

        return postItems.Select(FeedItemDto.FromPost)
            .Concat(quoteItems.Select(FeedItemDto.FromQuote))
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Post != null ? x.Post.Id : (x.Quote != null ? x.Quote.Id : 0))
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    /// <summary>Interleaves the two buckets following the plan's cadence, falling back to
    /// whichever bucket still has items so the page is filled even when one runs short.</summary>
    private static List<FeedItemDto> Weave(IReadOnlyList<bool> order, List<FeedItemDto> followed, List<FeedItemDto> others)
    {
        var items = new List<FeedItemDto>(order.Count);
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
