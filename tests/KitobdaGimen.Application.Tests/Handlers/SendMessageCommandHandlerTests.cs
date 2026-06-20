using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Chat.Commands.SendMessage;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class SendMessageCommandHandlerTests : TestBase
{
    private static async Task SeedUsersAsync(TestDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Adds an accepted connection so the two users pass the chat gate.</summary>
    private static async Task ConnectAsync(TestDbContext db, int requesterId, int addresseeId)
    {
        db.Connections.Add(new Connection
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = ConnectionStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Sending_to_recipient_creates_canonical_conversation_and_notifies_other()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 5, 3); // sender 5, recipient 3
        await ConnectAsync(db, 5, 3);
        var notifier = new SpyChatNotifier();
        var handler = new SendMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), notifier);

        var dto = await handler.Handle(new SendMessageCommand { RecipientId = 3, Text = "Salom" }, CancellationToken.None);

        Assert.Equal("Salom", dto.Text);
        Assert.True(dto.IsMine);

        var conv = await db.Conversations.SingleAsync();
        Assert.Equal(3, conv.User1Id); // smaller id stored as User1Id
        Assert.Equal(5, conv.User2Id);

        // recipient gets a push with IsMine = false
        Assert.Single(notifier.Sent);
        Assert.Equal(3, notifier.Sent[0].RecipientUserId);
        Assert.False(notifier.Sent[0].Message.IsMine);
    }

    [Fact]
    public async Task Reuses_existing_conversation()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 5, 3);
        await ConnectAsync(db, 5, 3);
        var handler = new SendMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), new SpyChatNotifier());

        await handler.Handle(new SendMessageCommand { RecipientId = 3, Text = "Birinchi" }, CancellationToken.None);
        await handler.Handle(new SendMessageCommand { RecipientId = 3, Text = "Ikkinchi" }, CancellationToken.None);

        Assert.Single(await db.Conversations.ToListAsync());
        Assert.Equal(2, await db.Messages.CountAsync());
    }

    [Fact]
    public async Task Cannot_message_without_accepted_connection()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 5, 3); // no connection between them
        var handler = new SendMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new SendMessageCommand { RecipientId = 3, Text = "Salom" }, CancellationToken.None));
    }

    [Fact]
    public async Task Cannot_message_self()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 5);
        var handler = new SendMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new SendMessageCommand { RecipientId = 5, Text = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task Cannot_send_to_conversation_user_is_not_part_of()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2, 3);
        db.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var handler = new SendMessageCommandHandler(db, new FakeCurrentUserService(userId: 3), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new SendMessageCommand { ConversationId = 1, Text = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_recipient_missing()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 5);
        var handler = new SendMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), new SpyChatNotifier());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new SendMessageCommand { RecipientId = 999, Text = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_shared_post_missing()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 5, 3);
        await ConnectAsync(db, 5, 3);
        var handler = new SendMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), new SpyChatNotifier());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new SendMessageCommand { RecipientId = 3, SharedPostId = 42 }, CancellationToken.None));
    }
}
