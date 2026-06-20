using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Auth.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return null;
        }

        return await _db.Users
            .Where(u => u.Id == userId)
            .ProjectToType<UserDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
