using MediatR;

namespace KitobdaGimen.Application.Features.Notifications.Commands.MarkNotificationsReadByUrl;

/// <summary>
/// Marks the current user's unread notifications that point to a given URL as read. Used when a
/// user opens a notification whose target (e.g. a post) has since been deleted — the stale
/// notification is cleared so it no longer shows as unread.
/// </summary>
public record MarkNotificationsReadByUrlCommand(string Url) : IRequest<Unit>;
