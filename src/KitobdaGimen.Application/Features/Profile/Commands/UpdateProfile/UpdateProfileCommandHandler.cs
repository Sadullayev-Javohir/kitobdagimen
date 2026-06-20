using FluentValidation.Results;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Profile.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = KitobdaGimen.Application.Common.Exceptions.ValidationException;

namespace KitobdaGimen.Application.Features.Profile.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", userId);

        var username = request.Username.Trim().ToLowerInvariant();

        var isTaken = await _db.Users.AnyAsync(
            u => u.Id != userId && u.Username == username, cancellationToken);
        if (isTaken)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Username), "Bu username band — boshqasini tanlang.")
            });
        }

        user.Username = username;
        user.FullName = request.FullName.Trim();
        user.Bio = request.Bio;
        user.AvatarUrl = request.AvatarUrl;

        await _db.SaveChangesAsync(cancellationToken);

        return new ProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            PostCount = await _db.Posts.CountAsync(p => p.UserId == userId, cancellationToken),
            FollowerCount = await _db.Follows.CountAsync(f => f.FollowingId == userId, cancellationToken),
            FollowingCount = await _db.Follows.CountAsync(f => f.FollowerId == userId, cancellationToken),
            IsFollowedByCurrentUser = false,
            IsCurrentUser = true
        };
    }
}
