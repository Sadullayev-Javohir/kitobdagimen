using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Connections.Commands.RespondToConnection;
using KitobdaGimen.Application.Features.Connections.Commands.SendConnectionRequest;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class ConnectionsHandlerTests : TestBase
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
    public async Task Send_creates_pending_and_notifies_addressee()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        var notif = new SpyNotificationService();
        var handler = new SendConnectionRequestCommandHandler(db, new FakeCurrentUserService(userId: 1), notif);

        var dto = await handler.Handle(new SendConnectionRequestCommand(2), CancellationToken.None);

        Assert.Equal(ConnectionStatus.Pending, dto.Status);
        Assert.True(dto.IamRequester);
        var conn = await db.Connections.SingleAsync();
        Assert.Equal(1, conn.RequesterId);
        Assert.Equal(2, conn.AddresseeId);
        Assert.Single(notif.Sent);
        Assert.Equal(2, notif.Sent[0].RecipientUserId);
        Assert.Equal("connection_request", notif.Sent[0].Notification.Type);
    }

    [Fact]
    public async Task Send_auto_accepts_reciprocal_pending_and_creates_conversation()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        // User 2 already invited user 1.
        db.Connections.Add(new Connection { RequesterId = 2, AddresseeId = 1, Status = ConnectionStatus.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var notif = new SpyNotificationService();
        var handler = new SendConnectionRequestCommandHandler(db, new FakeCurrentUserService(userId: 1), notif);

        var dto = await handler.Handle(new SendConnectionRequestCommand(2), CancellationToken.None);

        Assert.Equal(ConnectionStatus.Accepted, dto.Status);
        Assert.Single(await db.Connections.ToListAsync()); // reused, not duplicated
        Assert.Single(await db.Conversations.ToListAsync()); // conversation created on accept
        // Original requester (2) is told their invite was accepted.
        Assert.Contains(notif.Sent, n => n.RecipientUserId == 2 && n.Notification.Type == "connection_accepted");
    }

    [Fact]
    public async Task Cannot_invite_self()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1);
        var handler = new SendConnectionRequestCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new SendConnectionRequestCommand(1), CancellationToken.None));
    }

    [Fact]
    public async Task Respond_accept_marks_accepted_creates_conversation_and_notifies()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        db.Connections.Add(new Connection { Id = 7, RequesterId = 1, AddresseeId = 2, Status = ConnectionStatus.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var notif = new SpyNotificationService();
        var handler = new RespondToConnectionCommandHandler(db, new FakeCurrentUserService(userId: 2), notif);

        var dto = await handler.Handle(new RespondToConnectionCommand(7, true), CancellationToken.None);

        Assert.Equal(ConnectionStatus.Accepted, dto.Status);
        Assert.Single(await db.Conversations.ToListAsync());
        Assert.Contains(notif.Sent, n => n.RecipientUserId == 1 && n.Notification.Type == "connection_accepted");
    }

    [Fact]
    public async Task Respond_decline_does_not_create_conversation()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        db.Connections.Add(new Connection { Id = 8, RequesterId = 1, AddresseeId = 2, Status = ConnectionStatus.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var handler = new RespondToConnectionCommandHandler(db, new FakeCurrentUserService(userId: 2), new SpyNotificationService());

        var dto = await handler.Handle(new RespondToConnectionCommand(8, false), CancellationToken.None);

        Assert.Equal(ConnectionStatus.Declined, dto.Status);
        Assert.Empty(await db.Conversations.ToListAsync());
    }

    [Fact]
    public async Task Only_addressee_can_respond()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2);
        db.Connections.Add(new Connection { Id = 9, RequesterId = 1, AddresseeId = 2, Status = ConnectionStatus.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        // The requester (1) tries to accept their own invite.
        var handler = new RespondToConnectionCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new RespondToConnectionCommand(9, true), CancellationToken.None));
    }
}
