using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetPostLikes;

public class GetPostLikesQueryHandler : IRequestHandler<GetPostLikesQuery, PagedResult<PostLikerDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPostLikesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PostLikerDto>> Handle(GetPostLikesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var currentUserId = _currentUser.UserId;
        var viewerEmail = _currentUser.Email?.ToLowerInvariant();
        var now = DateTime.UtcNow;
        var today = UzTime.Today;

        var todayHeroUserId = await _db.DailyJackpots
            .Where(j => j.Date == today)
            .Select(j => (int?)j.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        var source = _db.Likes
            .Where(l => l.PostId == request.PostId)
            .OrderByDescending(l => l.CreatedAt);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new PostLikerDto
            {
                Id = l.User.Id,
                Username = l.User.Username,
                FullName = l.User.FullName,
                AvatarUrl = (l.User.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                             && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                             && viewerEmail != AvatarPrivacy.RestrictedEmail)
                    ? null
                    : l.User.AvatarUrl,
                Bio = l.User.Bio,
                IsFollowedByCurrentUser = currentUserId != null &&
                    l.User.Followers.Any(x => x.FollowerId == currentUserId),
                IsCurrentUser = currentUserId != null && l.User.Id == currentUserId,
                HasActiveStory = l.User.Stories.Any(s => s.ExpiresAt > now),
                IsDailyHero = todayHeroUserId.HasValue && l.User.Id == todayHeroUserId.Value,
                LikedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PostLikerDto>.Create(items, page, pageSize, totalCount);
    }
}
