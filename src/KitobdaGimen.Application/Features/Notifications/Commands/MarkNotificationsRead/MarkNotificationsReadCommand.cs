using MediatR;

namespace KitobdaGimen.Application.Features.Notifications.Commands.MarkNotificationsRead;

/// <summary>Marks the current user's notifications read. With no ids, marks ALL unread read
/// (used when the user opens /chat, where the bell points); with ids, marks just those.</summary>
public record MarkNotificationsReadCommand(IReadOnlyList<int>? Ids = null) : IRequest<Unit>;
