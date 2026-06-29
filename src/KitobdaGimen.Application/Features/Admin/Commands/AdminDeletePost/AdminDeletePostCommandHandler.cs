using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin.Commands.AdminDeletePost;

public class AdminDeletePostCommandHandler : IRequestHandler<AdminDeletePostCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminDeletePostCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(AdminDeletePostCommand request, CancellationToken cancellationToken)
    {
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.Admin, cancellationToken);

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId);

        // Likes/Comments/Views cascade from the post at the DB.
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
