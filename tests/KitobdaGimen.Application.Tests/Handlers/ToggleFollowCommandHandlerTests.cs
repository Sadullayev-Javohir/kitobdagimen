using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Follow.Commands.ToggleFollow;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class ToggleFollowCommandHandlerTests : TestBase
{
    private static async Task SeedUsersAsync(TestDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Follow_then_unfollow_updates_count_and_notifies_once()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        var notifier = new SpyNotificationService();
        var handler = new ToggleFollowCommandHandler(db, new FakeCurrentUserService(userId: 1), notifier);

        var followed = await handler.Handle(new ToggleFollowCommand(2), CancellationToken.None);
        Assert.True(followed.IsFollowing);
        Assert.Equal(1, followed.FollowerCount);
        Assert.Single(notifier.Sent);
        Assert.Equal(2, notifier.Sent[0].RecipientUserId);
        Assert.Equal("follow", notifier.Sent[0].Notification.Type);

        var unfollowed = await handler.Handle(new ToggleFollowCommand(2), CancellationToken.None);
        Assert.False(unfollowed.IsFollowing);
        Assert.Equal(0, unfollowed.FollowerCount);
        Assert.Single(notifier.Sent); // unfollow does not notify
    }

    [Fact]
    public async Task Cannot_follow_self()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1);
        var handler = new ToggleFollowCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new ToggleFollowCommand(1), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_target_missing()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1);
        var handler = new ToggleFollowCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ToggleFollowCommand(999), CancellationToken.None));
    }
}
