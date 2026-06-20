using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Profile.Queries.GetUserByUsername;

public class GetUserIdByUsernameQueryHandler : IRequestHandler<GetUserIdByUsernameQuery, int>
{
    private readonly IAppDbContext _db;

    public GetUserIdByUsernameQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<int> Handle(GetUserIdByUsernameQuery request, CancellationToken cancellationToken)
    {
        var username = (request.Username ?? string.Empty).Trim().ToLower();

        var id = await _db.Users
            .Where(u => u.Username != null && u.Username.ToLower() == username)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id ?? throw new NotFoundException("Foydalanuvchi", request.Username ?? string.Empty);
    }
}
