using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Commands.MarkMessagesRead;

public class MarkMessagesReadCommandHandler : IRequestHandler<MarkMessagesReadCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IChatNotifier _chatNotifier;

    public MarkMessagesReadCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IChatNotifier chatNotifier)
    {
        _db = db;
        _currentUser = currentUser;
        _chatNotifier = chatNotifier;
    }

    public async Task<Unit> Handle(MarkMessagesReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Suhbat", request.ConversationId);

        if (conversation.User1Id != userId && conversation.User2Id != userId)
        {
            throw new ForbiddenAccessException();
        }

        var unread = await _db.Messages
            .Where(m => m.ConversationId == request.ConversationId
                        && m.SenderId != userId
                        && !m.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return Unit.Value;
        }

        foreach (var message in unread)
        {
            message.IsRead = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Notify the other participant (the sender of those messages) so their outgoing
        // messages flip to the blue double-tick in real time.
        var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
        await _chatNotifier.MessagesReadAsync(otherUserId, request.ConversationId, cancellationToken);

        return Unit.Value;
    }
}
