using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Follow.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Follow.Queries.GetFollowing;

public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, PagedResult<FollowUserDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFollowingQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<FollowUserDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var currentUserId = _currentUser.UserId;
        var viewerEmail = _currentUser.Email?.ToLowerInvariant();

        // Follow rows originating from this user; the followed person is the listed user.
        var source = _db.Follows
            .Where(f => f.FollowerId == request.UserId)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FollowUserDto
            {
                Id = f.Following.Id,
                Username = f.Following.Username,
                FullName = f.Following.FullName,
                AvatarUrl = (f.Following.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                             && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                             && viewerEmail != AvatarPrivacy.RestrictedEmail)
                    ? null
                    : f.Following.AvatarUrl,
                Bio = f.Following.Bio,
                IsFollowedByCurrentUser = currentUserId != null &&
                    f.Following.Followers.Any(x => x.FollowerId == currentUserId)
            })
            .ToListAsync(cancellationToken);

        return PagedResult<FollowUserDto>.Create(items, page, pageSize, totalCount);
    }
}
