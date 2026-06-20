using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Profile.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Profile.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, ProfileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUserProfileQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId;

        var profile = await _db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new ProfileDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                CreatedAt = u.CreatedAt,
                PostCount = u.Posts.Count,
                FollowerCount = u.Followers.Count,
                FollowingCount = u.Following.Count,
                IsFollowedByCurrentUser = currentUserId != null &&
                    u.Followers.Any(f => f.FollowerId == currentUserId),
                IsCurrentUser = currentUserId != null && u.Id == currentUserId,
                HasStory = u.Stories.Any(s => s.ExpiresAt > DateTime.UtcNow)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", request.UserId);

        return profile;
    }
}
