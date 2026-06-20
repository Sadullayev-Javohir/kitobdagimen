using KitobdaGimen.Application.Features.Notifications.Commands.MarkNotificationsRead;
using KitobdaGimen.Application.Features.Notifications.Queries.GetUnreadNotifications;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class NotificationsHandlerTests : TestBase
{
    private static async Task SeedUsersAsync(TestDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }

    private static Notification Notif(int recipientId, bool isRead, DateTime createdAt, string type = "connection_request")
        => new() { RecipientId = recipientId, Type = type, ActorName = "Someone", Message = "msg", IsRead = isRead, CreatedAt = createdAt };

    [Fact]
    public async Task Unread_returns_only_my_unread_newest_first()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        var t0 = new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
        db.Notifications.AddRange(
            Notif(1, isRead: false, t0),                    // mine, unread (older)
            Notif(1, isRead: false, t0.AddMinutes(5)),      // mine, unread (newer)
            Notif(1, isRead: true, t0.AddMinutes(10)),      // mine, already read -> excluded
            Notif(2, isRead: false, t0.AddMinutes(15)));    // someone else -> excluded
        await db.SaveChangesAsync();

        var handler = new GetUnreadNotificationsQueryHandler(db, new FakeCurrentUserService(userId: 1));
        var result = await handler.Handle(new GetUnreadNotificationsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].CreatedAt > result[1].CreatedAt); // newest first
        Assert.All(result, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task MarkRead_with_no_ids_marks_all_my_unread()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        var t0 = new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
        db.Notifications.AddRange(
            Notif(1, isRead: false, t0),
            Notif(1, isRead: false, t0.AddMinutes(1)),
            Notif(2, isRead: false, t0));      // other user's stays untouched
        await db.SaveChangesAsync();

        var handler = new MarkNotificationsReadCommandHandler(db, new FakeCurrentUserService(userId: 1));
        await handler.Handle(new MarkNotificationsReadCommand(), CancellationToken.None);

        Assert.Empty(await db.Notifications.Where(n => n.RecipientId == 1 && !n.IsRead).ToListAsync());
        Assert.Single(await db.Notifications.Where(n => n.RecipientId == 2 && !n.IsRead).ToListAsync());
    }

    [Fact]
    public async Task MarkRead_with_ids_marks_only_those()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1);
        var t0 = new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
        var a = Notif(1, isRead: false, t0);
        var b = Notif(1, isRead: false, t0.AddMinutes(1));
        db.Notifications.AddRange(a, b);
        await db.SaveChangesAsync();

        var handler = new MarkNotificationsReadCommandHandler(db, new FakeCurrentUserService(userId: 1));
        await handler.Handle(new MarkNotificationsReadCommand(new[] { a.Id }), CancellationToken.None);

        Assert.True((await db.Notifications.FindAsync(a.Id))!.IsRead);
        Assert.False((await db.Notifications.FindAsync(b.Id))!.IsRead);
    }
}
