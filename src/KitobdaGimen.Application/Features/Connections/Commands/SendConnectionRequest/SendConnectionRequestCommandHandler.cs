using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat;
using KitobdaGimen.Application.Features.Connections.Dtos;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Connections.Commands.SendConnectionRequest;

public class SendConnectionRequestCommandHandler
    : IRequestHandler<SendConnectionRequestCommand, ConnectionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public SendConnectionRequestCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<ConnectionDto> Handle(
        SendConnectionRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        if (request.AddresseeId == userId)
        {
            throw new ForbiddenAccessException("O'zingizni taklif qila olmaysiz.");
        }

        var addressee = await _db.Users
            .Where(u => u.Id == request.AddresseeId)
            .Select(u => new { u.Id, u.Username, u.FullName, u.AvatarUrl, u.Email })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", request.AddresseeId);

        var me = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Username, u.FullName, u.AvatarUrl, u.Email })
            .FirstAsync(cancellationToken);

        // Avatar maxfiyligi: joriy foydalanuvchi (viewer) uchun addressee avatari va
        // actor (me) avatari cheklangan foydalanuvchi bo'lsa yashiriladi.
        var viewerEmail = _currentUser.Email?.ToLowerInvariant();
        var addresseeAvatar = AvatarPrivacy.Resolve(addressee.Email, addressee.AvatarUrl, viewerEmail);
        var meAvatar = AvatarPrivacy.ForActor(viewerEmail, me.AvatarUrl);

        // Any existing connection between the two, in either direction.
        var existing = await _db.Connections.FirstOrDefaultAsync(
            c => (c.RequesterId == userId && c.AddresseeId == request.AddresseeId)
                 || (c.RequesterId == request.AddresseeId && c.AddresseeId == userId),
            cancellationToken);

        if (existing is not null)
        {
            // Already connected — no-op.
            if (existing.Status == ConnectionStatus.Accepted)
            {
                return ToDto(existing, userId, addressee.Id, addressee.Username, addressee.FullName, addresseeAvatar);
            }

            // The other user already invited me → auto-accept.
            if (existing.Status == ConnectionStatus.Pending && existing.AddresseeId == userId)
            {
                existing.Status = ConnectionStatus.Accepted;
                existing.RespondedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                await ConversationHelper.GetOrCreateAsync(_db, userId, request.AddresseeId, cancellationToken);

                // Tell the original requester their invite was accepted.
                await NotifyAcceptedAsync(existing.RequesterId, me.Id, me.FullName, meAvatar, cancellationToken);

                return ToDto(existing, userId, addressee.Id, addressee.Username, addressee.FullName, addresseeAvatar);
            }

            // I already sent a pending invite → return as-is.
            if (existing.Status == ConnectionStatus.Pending && existing.RequesterId == userId)
            {
                return ToDto(existing, userId, addressee.Id, addressee.Username, addressee.FullName, addresseeAvatar);
            }

            // Declined previously. Reuse the (me → other) row if that is the one that exists;
            // otherwise fall through to create a fresh (me → other) row.
            if (existing.RequesterId == userId && existing.AddresseeId == request.AddresseeId)
            {
                existing.Status = ConnectionStatus.Pending;
                existing.CreatedAt = DateTime.UtcNow;
                existing.RespondedAt = null;
                await _db.SaveChangesAsync(cancellationToken);
                await NotifyRequestAsync(existing, me.Id, me.FullName, meAvatar, cancellationToken);
                return ToDto(existing, userId, addressee.Id, addressee.Username, addressee.FullName, addresseeAvatar);
            }
        }

        var connection = new Connection
        {
            RequesterId = userId,
            AddresseeId = request.AddresseeId,
            Status = ConnectionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.Connections.Add(connection);
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyRequestAsync(connection, me.Id, me.FullName, meAvatar, cancellationToken);

        return ToDto(connection, userId, addressee.Id, addressee.Username, addressee.FullName, addresseeAvatar);
    }

    private Task NotifyRequestAsync(Connection connection, int actorId, string actorName, string? actorAvatar, CancellationToken ct)
        => _notifications.NotifyAsync(connection.AddresseeId, new NotificationDto
        {
            Type = "connection_request",
            RelatedId = connection.Id,
            ActorId = actorId,
            ActorName = actorName,
            ActorAvatarUrl = actorAvatar,
            Message = $"{actorName} sizni suhbatga taklif qildi",
            Url = "/chat"
        }, ct);

    private Task NotifyAcceptedAsync(int recipientId, int actorId, string actorName, string? actorAvatar, CancellationToken ct)
        => _notifications.NotifyAsync(recipientId, new NotificationDto
        {
            Type = "connection_accepted",
            ActorId = actorId,
            ActorName = actorName,
            ActorAvatarUrl = actorAvatar,
            Message = $"{actorName} taklifingizni qabul qildi",
            Url = "/chat"
        }, ct);

    private static ConnectionDto ToDto(
        Connection c, int currentUserId, int otherId, string? otherUsername, string otherFullName, string? otherAvatar)
        => new()
        {
            Id = c.Id,
            Status = c.Status,
            IamRequester = c.RequesterId == currentUserId,
            CreatedAt = c.CreatedAt,
            RespondedAt = c.RespondedAt,
            OtherUser = new UserSummaryDto
            {
                Id = otherId,
                Username = otherUsername,
                FullName = otherFullName,
                AvatarUrl = otherAvatar
            }
        };
}
