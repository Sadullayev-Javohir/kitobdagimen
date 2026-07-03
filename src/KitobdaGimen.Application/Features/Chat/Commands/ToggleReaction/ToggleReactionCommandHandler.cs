using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Chat.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Commands.ToggleReaction;

public class ToggleReactionCommandHandler : IRequestHandler<ToggleReactionCommand, MessageDto>
{
    /// <summary>Curated reaction set (matches the client picker) — prevents arbitrary content.</summary>
    private static readonly HashSet<string> AllowedEmojis = new()
    {
        "❤️", "👍", "👎", "😂", "😮", "😢", "🔥", "🎉", "📚", "🙏"
    };

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IChatNotifier _chatNotifier;

    public ToggleReactionCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IChatNotifier chatNotifier)
    {
        _db = db;
        _currentUser = currentUser;
        _chatNotifier = chatNotifier;
    }

    public async Task<MessageDto> Handle(ToggleReactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var emoji = request.Emoji?.Trim() ?? "";
        if (!AllowedEmojis.Contains(emoji))
        {
            throw new ForbiddenAccessException("Bunday reaksiya qo'llab-quvvatlanmaydi.");
        }

        var message = await _db.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException("Xabar", request.MessageId);

        var conversation = message.Conversation;
        if (conversation.User1Id != userId && conversation.User2Id != userId)
        {
            throw new ForbiddenAccessException();
        }

        var existing = await _db.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == message.Id && r.UserId == userId, cancellationToken);

        // True when the reaction is being ADDED or CHANGED (not toggled off) — drives the
        // "someone reacted to your message" notification to the message owner.
        var reactionApplied = false;
        if (existing is null)
        {
            _db.MessageReactions.Add(new MessageReaction
            {
                MessageId = message.Id,
                UserId = userId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow
            });
            reactionApplied = true;
        }
        else if (existing.Emoji == emoji)
        {
            // Same emoji tapped again → remove (toggle off).
            _db.MessageReactions.Remove(existing);
        }
        else
        {
            // Different emoji → replace the previous one.
            existing.Emoji = emoji;
            existing.CreatedAt = DateTime.UtcNow;
            reactionApplied = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.Messages
            .Where(m => m.Id == message.Id)
            .ToMessageDto(userId)
            .FirstAsync(cancellationToken);

        await _db.AttachReactionsAsync(new[] { dto }, userId, cancellationToken);

        // Push the reaction change to the other participant. Reactions and the IsMine flag are
        // viewer-specific, so re-project the message from the other user's perspective.
        var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

        var otherDto = await _db.Messages
            .Where(m => m.Id == message.Id)
            .ToMessageDto(otherUserId)
            .FirstAsync(cancellationToken);
        await _db.AttachReactionsAsync(new[] { otherDto }, otherUserId, cancellationToken);

        await _chatNotifier.MessageReactionChangedAsync(otherUserId, otherDto, cancellationToken);

        // Notify the message OWNER that someone reacted to their message (Telegram-style). Only when
        // the reaction was applied (not toggled off) and the reactor isn't reacting to their own message.
        if (reactionApplied && message.SenderId != userId)
        {
            var actor = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.AvatarUrl })
                .FirstOrDefaultAsync(cancellationToken);
            if (actor is not null)
            {
                await _chatNotifier.ReactionNotificationAsync(
                    message.SenderId, actor.FullName, actor.AvatarUrl, emoji, conversation.Id, cancellationToken);
            }
        }

        return dto;
    }
}
