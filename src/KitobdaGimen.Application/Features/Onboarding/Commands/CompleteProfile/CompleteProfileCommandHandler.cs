using FluentValidation.Results;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Auth.Dtos;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Onboarding.Commands.CompleteProfile;

public class CompleteProfileCommandHandler : IRequestHandler<CompleteProfileCommand, UserDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public CompleteProfileCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    {
        _db = db;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CompleteProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", userId);

        var username = request.Username.Trim().ToLowerInvariant();

        var isTaken = await _db.Users.AnyAsync(u => u.Id != userId && u.Username == username, cancellationToken);
        if (isTaken)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Username), "Bu username band — boshqasini tanlang.")
            });
        }

        user.Username = username;
        user.FullName = request.FullName.Trim();
        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
