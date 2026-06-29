using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin.Commands.SetUserRole;

public class SetUserRoleCommandHandler : IRequestHandler<SetUserRoleCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetUserRoleCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        var (callerId, _) = await AdminGuard.RequireAsync(_db, _currentUser, UserRole.SuperAdmin, cancellationToken);

        if (request.TargetUserId == callerId)
        {
            throw new ForbiddenAccessException("O'z rolingizni o'zgartira olmaysiz.");
        }

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", request.TargetUserId);

        // Boshqa super adminlarni bu yo'l bilan o'zgartirib bo'lmaydi.
        if (target.Role == UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Super admin rolini o'zgartirib bo'lmaydi.");
        }

        target.Role = request.MakeAdmin ? UserRole.Admin : UserRole.User;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
