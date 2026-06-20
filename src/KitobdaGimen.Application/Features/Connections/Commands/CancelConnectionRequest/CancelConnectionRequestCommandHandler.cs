using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Connections.Commands.CancelConnectionRequest;

public class CancelConnectionRequestCommandHandler
    : IRequestHandler<CancelConnectionRequestCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelConnectionRequestCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(CancelConnectionRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == request.ConnectionId, cancellationToken)
            ?? throw new NotFoundException("Taklif", request.ConnectionId);

        if (connection.RequesterId != userId)
        {
            throw new ForbiddenAccessException("Bu taklifni bekor qila olmaysiz.");
        }

        if (connection.Status != ConnectionStatus.Pending)
        {
            throw new ForbiddenAccessException("Faqat javob berilmagan taklifni bekor qilish mumkin.");
        }

        _db.Connections.Remove(connection);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
