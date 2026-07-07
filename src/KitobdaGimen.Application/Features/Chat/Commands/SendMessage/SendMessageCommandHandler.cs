using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IChatNotifier _chatNotifier;
    private readonly IPushSender _push;

    public SendMessageCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IChatNotifier chatNotifier, IPushSender push)
    {
        _db = db;
        _currentUser = currentUser;
        _chatNotifier = chatNotifier;
        _push = push;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        Conversation conversation;
        if (request.ConversationId is int conversationId)
        {
            conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                ?? throw new NotFoundException("Suhbat", conversationId);

            if (conversation.User1Id != userId && conversation.User2Id != userId)
            {
                throw new ForbiddenAccessException();
            }
        }
        else
        {
            var recipientId = request.RecipientId!.Value;
            if (recipientId == userId)
            {
                throw new ForbiddenAccessException("O'zingizga xabar yubora olmaysiz.");
            }

            var recipientExists = await _db.Users.AnyAsync(u => u.Id == recipientId, cancellationToken);
            if (!recipientExists)
            {
                throw new NotFoundException("Foydalanuvchi", recipientId);
            }

            conversation = await ConversationHelper.GetOrCreateAsync(_db, userId, recipientId, cancellationToken);
        }

        // Gate: the two participants must have an accepted connection before they can chat.
        var otherId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
        var isConnected = await _db.Connections.AnyAsync(
            c => c.Status == ConnectionStatus.Accepted
                 && ((c.RequesterId == userId && c.AddresseeId == otherId)
                     || (c.RequesterId == otherId && c.AddresseeId == userId)),
            cancellationToken);
        if (!isConnected)
        {
            throw new ForbiddenAccessException("Avval taklif yuborib, qabul qilinishini kuting.");
        }

        if (request.SharedPostId is int sharedPostId)
        {
            var postExists = await _db.Posts.AnyAsync(p => p.Id == sharedPostId, cancellationToken);
            if (!postExists)
            {
                throw new NotFoundException("Post", sharedPostId);
            }
        }

        // Reply target: must be an existing message in the SAME conversation (prevents quoting
        // across conversations or referencing arbitrary ids).
        int? replyToId = null;
        if (request.ReplyToMessageId is int replyId)
        {
            var replyValid = await _db.Messages.AnyAsync(
                m => m.Id == replyId && m.ConversationId == conversation.Id, cancellationToken);
            if (replyValid)
            {
                replyToId = replyId;
            }
        }

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Text = string.IsNullOrWhiteSpace(request.Text) ? null : request.Text,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl,
            StickerKey = string.IsNullOrWhiteSpace(request.StickerKey) ? null : request.StickerKey.Trim(),
            VoiceUrl = string.IsNullOrWhiteSpace(request.VoiceUrl) ? null : request.VoiceUrl,
            VoiceDurationSeconds = request.VoiceDurationSeconds,
            SharedPostId = request.SharedPostId,
            ReplyToMessageId = replyToId,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.Messages
            .Where(m => m.Id == message.Id)
            .ToMessageDto(userId)
            .FirstAsync(cancellationToken);

        // Push the message to the other participant in real time (from their perspective IsMine = false).
        var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
        await _chatNotifier.MessageReceivedAsync(otherUserId, dto with { IsMine = false }, cancellationToken);

        // Light up the recipient's navbar "Xabarlar" badge on any page (global notification hub).
        await _chatNotifier.NewMessageBadgeAsync(
            otherUserId, conversation.Id, dto.Sender.FullName, dto.Sender.AvatarUrl, cancellationToken);

        // Real device notification (Web Push → phone/system notification tray, even when the app
        // is closed or in the background). Shows WHO sent the message + a short preview. Best-effort.
        await _push.SendAsync(otherUserId, new PushNotificationPayload
        {
            Title = string.IsNullOrWhiteSpace(dto.Sender.FullName) ? "Yangi xabar" : dto.Sender.FullName,
            Body = BuildMessagePreview(message),
            Url = "/chat",
            Icon = string.IsNullOrWhiteSpace(dto.Sender.AvatarUrl) ? "/img/icons/icon-192.png" : dto.Sender.AvatarUrl,
            // Per-conversation tag so successive messages from the same person collapse/re-notify
            // instead of stacking up in the tray.
            Tag = $"chat-{conversation.Id}"
        }, cancellationToken);

        return dto;
    }

    /// <summary>A short, notification-friendly preview of what was sent (text/photo/voice/etc.).</summary>
    private static string BuildMessagePreview(Message message)
    {
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            var text = message.Text.Trim();
            return text.Length > 120 ? text[..120] + "…" : text;
        }
        if (!string.IsNullOrWhiteSpace(message.ImageUrl)) return "📷 Rasm";
        if (!string.IsNullOrWhiteSpace(message.VoiceUrl)) return "🎤 Ovozli xabar";
        if (!string.IsNullOrWhiteSpace(message.StickerKey)) return "Stiker";
        if (message.SharedPostId is not null) return "Post ulashdi";
        return "Yangi xabar";
    }
}
