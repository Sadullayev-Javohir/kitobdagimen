using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Commands.DeleteMessage;

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IChatNotifier _chatNotifier;

    public DeleteMessageCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IChatNotifier chatNotifier)
    {
        _db = db;
        _currentUser = currentUser;
        _chatNotifier = chatNotifier;
    }

    public async Task Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var message = await _db.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException("Xabar", request.MessageId);

        // Only the author may delete their own message.
        if (message.SenderId != userId)
        {
            throw new ForbiddenAccessException("Faqat o'z xabaringizni o'chira olasiz.");
        }

        var conversationId = message.ConversationId;
        var otherUserId = message.Conversation.User1Id == userId
            ? message.Conversation.User2Id
            : message.Conversation.User1Id;

        // Soft-delete: hide from both participants but keep the row so a super admin can still
        // audit the message (the chat-monitoring view shows it marked as deleted).
        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _chatNotifier.MessageDeletedAsync(otherUserId, conversationId, request.MessageId, cancellationToken);
    }
}
