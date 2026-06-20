using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Chat.Commands.DeleteMessage;
using KitobdaGimen.Application.Features.Chat.Commands.EditMessage;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class EditDeleteMessageHandlerTests : TestBase
{
    /// <summary>Seeds two users, a conversation between them and one message sent by <paramref name="senderId"/>.</summary>
    private static async Task<int> SeedMessageAsync(TestDbContext db, int senderId, int otherId, string text)
    {
        foreach (var id in new[] { senderId, otherId })
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        db.Conversations.Add(new Conversation { Id = 1, User1Id = Math.Min(senderId, otherId), User2Id = Math.Max(senderId, otherId), CreatedAt = DateTime.UtcNow });
        var message = new Message { Id = 1, ConversationId = 1, SenderId = senderId, Text = text, SentAt = DateTime.UtcNow };
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        return message.Id;
    }

    [Fact]
    public async Task Owner_can_edit_their_message_and_other_is_notified()
    {
        using var db = CreateContext();
        var id = await SeedMessageAsync(db, senderId: 5, otherId: 3, text: "Eski");
        var notifier = new SpyChatNotifier();
        var handler = new EditMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), notifier);

        var dto = await handler.Handle(new EditMessageCommand(id, "Yangi"), CancellationToken.None);

        Assert.Equal("Yangi", dto.Text);
        Assert.NotNull(dto.EditedAt);
        Assert.True(dto.IsMine);
        Assert.Equal("Yangi", (await db.Messages.SingleAsync()).Text);

        Assert.Single(notifier.Edited);
        Assert.Equal(3, notifier.Edited[0].RecipientUserId);
        Assert.False(notifier.Edited[0].Message.IsMine);
    }

    [Fact]
    public async Task Cannot_edit_someone_elses_message()
    {
        using var db = CreateContext();
        var id = await SeedMessageAsync(db, senderId: 5, otherId: 3, text: "Eski");
        var handler = new EditMessageCommandHandler(db, new FakeCurrentUserService(userId: 3), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new EditMessageCommand(id, "Yangi"), CancellationToken.None));
    }

    [Fact]
    public async Task Cannot_edit_to_empty_text()
    {
        using var db = CreateContext();
        var id = await SeedMessageAsync(db, senderId: 5, otherId: 3, text: "Eski");
        var handler = new EditMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new EditMessageCommand(id, "   "), CancellationToken.None));
    }

    [Fact]
    public async Task Owner_can_delete_their_message_and_other_is_notified()
    {
        using var db = CreateContext();
        var id = await SeedMessageAsync(db, senderId: 5, otherId: 3, text: "Salom");
        var notifier = new SpyChatNotifier();
        var handler = new DeleteMessageCommandHandler(db, new FakeCurrentUserService(userId: 5), notifier);

        await handler.Handle(new DeleteMessageCommand(id), CancellationToken.None);

        Assert.Empty(await db.Messages.ToListAsync());
        Assert.Single(notifier.Deleted);
        Assert.Equal(3, notifier.Deleted[0].RecipientUserId);
        Assert.Equal(id, notifier.Deleted[0].MessageId);
    }

    [Fact]
    public async Task Cannot_delete_someone_elses_message()
    {
        using var db = CreateContext();
        var id = await SeedMessageAsync(db, senderId: 5, otherId: 3, text: "Salom");
        var handler = new DeleteMessageCommandHandler(db, new FakeCurrentUserService(userId: 3), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new DeleteMessageCommand(id), CancellationToken.None));

        Assert.Single(await db.Messages.ToListAsync()); // still there
    }
}
