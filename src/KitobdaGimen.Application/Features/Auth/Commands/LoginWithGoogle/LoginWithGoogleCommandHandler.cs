using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Auth.Dtos;
using KitobdaGimen.Domain.Entities;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandHandler : IRequestHandler<LoginWithGoogleCommand, AuthResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public LoginWithGoogleCommandHandler(IAppDbContext db, ITokenService tokenService, IMapper mapper)
    {
        _db = db;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<AuthResultDto> Handle(LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.GoogleId == request.GoogleId, cancellationToken);

        var isNewUser = user is null;

        if (user is null)
        {
            user = new User
            {
                GoogleId = request.GoogleId,
                Email = request.Email,
                FullName = request.FullName,
                AvatarUrl = request.AvatarUrl,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Keep the basic profile in sync with Google on each login.
            user.Email = request.Email;
            user.FullName = request.FullName;
            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        var hasGenres = !isNewUser &&
            await _db.UserGenres.AnyAsync(ug => ug.UserId == user.Id, cancellationToken);

        var token = _tokenService.GenerateToken(user);

        return new AuthResultDto
        {
            Token = token,
            User = _mapper.Map<UserDto>(user),
            RequiresOnboarding = !hasGenres,
            RequiresProfileSetup = string.IsNullOrEmpty(user.Username)
        };
    }
}
