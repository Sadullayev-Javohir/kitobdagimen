using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetUnreadMessageCount;

public class GetUnreadMessageCountQueryHandler : IRequestHandler<GetUnreadMessageCountQuery, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadMessageCountQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(GetUnreadMessageCountQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        // Unread incoming messages across every conversation the user takes part in.
        // Soft-deleted messages are excluded — they are no longer visible to the recipient.
        return await _db.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .SelectMany(c => c.Messages)
            .CountAsync(m => m.SenderId != userId && !m.IsRead && !m.IsDeleted, cancellationToken);
    }
}
