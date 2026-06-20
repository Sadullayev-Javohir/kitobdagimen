using KitobdaGimen.Application.Common.Models;
using MediatR;

namespace KitobdaGimen.Application.Features.Notifications.Queries.GetUnreadNotifications;

/// <summary>Unread notifications for the current user (newest first). Drives the navbar badge and
/// lets the client replay invites/notifications missed while the user was offline.</summary>
public record GetUnreadNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;
