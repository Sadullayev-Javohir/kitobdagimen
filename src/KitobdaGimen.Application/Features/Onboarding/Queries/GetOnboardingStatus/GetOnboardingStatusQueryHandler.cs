using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Onboarding.Queries.GetOnboardingStatus;

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOnboardingStatusQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var username = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync(cancellationToken);

        var hasGenres = await _db.UserGenres
            .AnyAsync(ug => ug.UserId == userId, cancellationToken);

        return new OnboardingStatusDto
        {
            HasUsername = !string.IsNullOrEmpty(username),
            HasGenres = hasGenres
        };
    }
}
