using FluentValidation.Results;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin.Commands.BroadcastNotification;

public class BroadcastNotificationCommandHandler : IRequestHandler<BroadcastNotificationCommand, int>
{
    /// <summary>Bir e'londa qat'iy chegaralar (bazadagi ustun uzunliklariga mos).</summary>
    private const int MaxTitle = 150;
    private const int MaxMessage = 300;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public BroadcastNotificationCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<int> Handle(BroadcastNotificationCommand request, CancellationToken cancellationToken)
    {
        // Faqat super admin barcha foydalanuvchilarga xabar yubora oladi.
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.SuperAdmin, cancellationToken);

        var title = (request.Title ?? "").Trim();
        var message = (request.Message ?? "").Trim();
        if (title.Length == 0)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(request.Title), "Sarlavha bo'sh bo'lishi mumkin emas.") });
        }
        if (message.Length == 0)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(request.Message), "Xabar matni bo'sh bo'lishi mumkin emas.") });
        }
        if (title.Length > MaxTitle) title = title[..MaxTitle];
        if (message.Length > MaxMessage) message = message[..MaxMessage];

        var url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim();

        var recipientIds = await _db.Users
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        if (recipientIds.Count == 0)
        {
            return 0;
        }

        // NotifyManyAsync har bir foydalanuvchi uchun satr saqlaydi VA SignalR orqali live yuboradi —
        // shu sabab onlayn foydalanuvchilar refreshsiz, real-time ko'radi.
        await _notifications.NotifyManyAsync(recipientIds, new NotificationDto
        {
            Type = "announcement",
            Title = title,
            Message = message,
            Url = url,
            ActorName = "kitobdagimen.uz"
        }, cancellationToken);

        return recipientIds.Count;
    }
}
