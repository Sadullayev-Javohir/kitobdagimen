using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Follow.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Follow.Queries.GetFollowers;

public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, PagedResult<FollowUserDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFollowersQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<FollowUserDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var currentUserId = _currentUser.UserId;

        // Follow rows pointing at this user; the follower is the listed user.
        var source = _db.Follows
            .Where(f => f.FollowingId == request.UserId)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FollowUserDto
            {
                Id = f.Follower.Id,
                Username = f.Follower.Username,
                FullName = f.Follower.FullName,
                AvatarUrl = f.Follower.AvatarUrl,
                Bio = f.Follower.Bio,
                IsFollowedByCurrentUser = currentUserId != null &&
                    f.Follower.Followers.Any(x => x.FollowerId == currentUserId)
            })
            .ToListAsync(cancellationToken);

        return PagedResult<FollowUserDto>.Create(items, page, pageSize, totalCount);
    }
}
