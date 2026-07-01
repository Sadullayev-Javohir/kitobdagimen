using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Commands.EditMessage;

public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, MessageDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IChatNotifier _chatNotifier;

    public EditMessageCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IChatNotifier chatNotifier)
    {
        _db = db;
        _currentUser = currentUser;
        _chatNotifier = chatNotifier;
    }

    public async Task<MessageDto> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var text = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ForbiddenAccessException("Xabar matni bo'sh bo'lishi mumkin emas.");
        }
        if (text.Length > 5000)
        {
            throw new ForbiddenAccessException("Xabar 5000 belgidan oshmasligi kerak.");
        }

        var message = await _db.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException("Xabar", request.MessageId);

        // Only the author may edit their own message.
        if (message.SenderId != userId)
        {
            throw new ForbiddenAccessException("Faqat o'z xabaringizni tahrirlay olasiz.");
        }

        message.Text = text;
        message.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.Messages
            .Where(m => m.Id == message.Id)
            .ToMessageDto(userId)
            .FirstAsync(cancellationToken);

        await _db.AttachReactionsAsync(new[] { dto }, userId, cancellationToken);

        var otherUserId = message.Conversation.User1Id == userId
            ? message.Conversation.User2Id
            : message.Conversation.User1Id;
        await _chatNotifier.MessageEditedAsync(otherUserId, dto with { IsMine = false }, cancellationToken);

        return dto;
    }
}
