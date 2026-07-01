using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat;
using KitobdaGimen.Application.Features.Connections.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Connections.Commands.RespondToConnection;

public class RespondToConnectionCommandHandler
    : IRequestHandler<RespondToConnectionCommand, ConnectionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public RespondToConnectionCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<ConnectionDto> Handle(
        RespondToConnectionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == request.ConnectionId, cancellationToken)
            ?? throw new NotFoundException("Taklif", request.ConnectionId);

        // Only the addressee may accept/decline.
        if (connection.AddresseeId != userId)
        {
            throw new ForbiddenAccessException("Bu taklifga javob bera olmaysiz.");
        }

        if (connection.Status != ConnectionStatus.Pending)
        {
            throw new ForbiddenAccessException("Bu taklifga allaqachon javob berilgan.");
        }

        connection.Status = request.Accept ? ConnectionStatus.Accepted : ConnectionStatus.Declined;
        connection.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var requester = await _db.Users
            .Where(u => u.Id == connection.RequesterId)
            .Select(u => new { u.Id, u.Username, u.FullName, u.AvatarUrl, u.Email })
            .FirstAsync(cancellationToken);

        var viewerEmail = _currentUser.Email?.ToLowerInvariant();

        if (request.Accept)
        {
            await ConversationHelper.GetOrCreateAsync(_db, userId, connection.RequesterId, cancellationToken);

            var me = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.AvatarUrl })
                .FirstAsync(cancellationToken);

            await _notifications.NotifyAsync(connection.RequesterId, new NotificationDto
            {
                Type = "connection_accepted",
                ActorId = userId,
                ActorName = me.FullName,
                ActorAvatarUrl = AvatarPrivacy.ForActor(viewerEmail, me.AvatarUrl),
                Message = $"{me.FullName} taklifingizni qabul qildi",
                Url = "/chat"
            }, cancellationToken);
        }

        return new ConnectionDto
        {
            Id = connection.Id,
            Status = connection.Status,
            IamRequester = false,
            CreatedAt = connection.CreatedAt,
            RespondedAt = connection.RespondedAt,
            OtherUser = new UserSummaryDto
            {
                Id = requester.Id,
                Username = requester.Username,
                FullName = requester.FullName,
                AvatarUrl = AvatarPrivacy.Resolve(requester.Email, requester.AvatarUrl, viewerEmail)
            }
        };
    }
}
