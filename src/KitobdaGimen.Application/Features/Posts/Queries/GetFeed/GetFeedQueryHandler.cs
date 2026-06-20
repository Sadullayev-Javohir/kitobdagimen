using KitobdaGimen.Application.Common.Feed;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetFeed;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, PagedResult<PostDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFeedQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PostDto>> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var userId = _currentUser.UserId;
        var hasSearch = !string.IsNullOrWhiteSpace(request.Search);

        // Search scans ALL posts by recency — the follow blend doesn't apply.
        if (hasSearch)
        {
            // ToLower().Contains keeps this provider-agnostic (no ILike in Application layer).
            var term = request.Search!.Trim().ToLower();
            var matches = _db.Posts.Where(p =>
                p.ReviewText.ToLower().Contains(term) ||
                p.Book.Title.ToLower().Contains(term) ||
                p.Book.Author.ToLower().Contains(term) ||
                p.User.FullName.ToLower().Contains(term));

            return await PageByRecencyAsync(matches, page, pageSize, userId, cancellationToken);
        }

        var followingIds = userId is null
            ? new List<int>()
            : await _db.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync(cancellationToken);

        // Following no one (or anonymous) → plain global feed: recent posts from many users.
        if (followingIds.Count == 0)
        {
            return await PageByRecencyAsync(_db.Posts, page, pageSize, userId, cancellationToken);
        }

        // Two recency-ordered buckets: followed authors (+ the user's own) and everyone else.
        var followed = _db.Posts.Where(p => followingIds.Contains(p.UserId) || p.UserId == userId);
        var others = _db.Posts.Where(p => !followingIds.Contains(p.UserId) && p.UserId != userId);

        var followedTotal = await followed.CountAsync(cancellationToken);
        var othersTotal = await others.CountAsync(cancellationToken);

        var plan = FollowMixPlan.Build(page, pageSize, followedTotal, othersTotal);

        var followedItems = plan.FollowedTake > 0
            ? await TakeByRecencyAsync(followed, plan.FollowedSkip, plan.FollowedTake, userId, cancellationToken)
            : new List<PostDto>();
        var otherItems = plan.NonFollowedTake > 0
            ? await TakeByRecencyAsync(others, plan.NonFollowedSkip, plan.NonFollowedTake, userId, cancellationToken)
            : new List<PostDto>();

        var items = Weave(plan.Order, followedItems, otherItems);
        return PagedResult<PostDto>.Create(items, page, pageSize, followedTotal + othersTotal);
    }

    private async Task<PagedResult<PostDto>> PageByRecencyAsync(
        IQueryable<Post> source, int page, int pageSize, int? userId, CancellationToken ct)
    {
        var totalCount = await source.CountAsync(ct);
        var items = await TakeByRecencyAsync(source, (page - 1) * pageSize, pageSize, userId, ct);
        return PagedResult<PostDto>.Create(items, page, pageSize, totalCount);
    }

    private static Task<List<PostDto>> TakeByRecencyAsync(
        IQueryable<Post> source, int skip, int take, int? userId, CancellationToken ct)
        => source
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToPostDto(userId)
            .ToListAsync(ct);

    /// <summary>Interleaves the two buckets following the plan's cadence, falling back to
    /// whichever bucket still has items so the page is filled even when one runs short.</summary>
    private static List<PostDto> Weave(IReadOnlyList<bool> order, List<PostDto> followed, List<PostDto> others)
    {
        var items = new List<PostDto>(order.Count);
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
