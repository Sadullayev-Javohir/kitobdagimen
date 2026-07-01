using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Chat.Commands.ToggleReaction;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class ToggleReactionCommandHandlerTests : TestBase
{
    private static async Task SeedAsync(TestDbContext db)
    {
        db.Users.Add(new User { Id = 1, GoogleId = "g-1", Email = "u1@e.com", FullName = "U1", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new User { Id = 2, GoogleId = "g-2", Email = "u2@e.com", FullName = "U2", CreatedAt = DateTime.UtcNow });
        db.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
        db.Messages.Add(new Message { Id = 10, ConversationId = 1, SenderId = 2, Text = "Salom", SentAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Adds_reaction_and_notifies_other_participant()
    {
        using var db = CreateContext();
        await SeedAsync(db);
        var notifier = new SpyChatNotifier();
        var handler = new ToggleReactionCommandHandler(db, new FakeCurrentUserService(userId: 1), notifier);

        var dto = await handler.Handle(new ToggleReactionCommand(10, "❤️"), CancellationToken.None);

        var group = Assert.Single(dto.Reactions);
        Assert.Equal("❤️", group.Emoji);
        Assert.Equal(1, group.Count);
        Assert.True(group.Mine);

        Assert.Equal(1, await db.MessageReactions.CountAsync());
        // The other participant (user 2) gets the live reaction push.
        var push = Assert.Single(notifier.Reactions);
        Assert.Equal(2, push.RecipientUserId);
    }

    [Fact]
    public async Task Tapping_same_emoji_again_removes_the_reaction()
    {
        using var db = CreateContext();
        await SeedAsync(db);
        var handler = new ToggleReactionCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyChatNotifier());

        await handler.Handle(new ToggleReactionCommand(10, "👍"), CancellationToken.None);
        var dto = await handler.Handle(new ToggleReactionCommand(10, "👍"), CancellationToken.None);

        Assert.Empty(dto.Reactions);
        Assert.Equal(0, await db.MessageReactions.CountAsync());
    }

    [Fact]
    public async Task Different_emoji_replaces_previous_reaction()
    {
        using var db = CreateContext();
        await SeedAsync(db);
        var handler = new ToggleReactionCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyChatNotifier());

        await handler.Handle(new ToggleReactionCommand(10, "👍"), CancellationToken.None);
        var dto = await handler.Handle(new ToggleReactionCommand(10, "🔥"), CancellationToken.None);

        var group = Assert.Single(dto.Reactions);
        Assert.Equal("🔥", group.Emoji);
        Assert.Equal(1, await db.MessageReactions.CountAsync());
    }

    [Fact]
    public async Task Rejects_unsupported_emoji()
    {
        using var db = CreateContext();
        await SeedAsync(db);
        var handler = new ToggleReactionCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new ToggleReactionCommand(10, "💩"), CancellationToken.None));
    }

    [Fact]
    public async Task Non_participant_cannot_react()
    {
        using var db = CreateContext();
        await SeedAsync(db);
        db.Users.Add(new User { Id = 3, GoogleId = "g-3", Email = "u3@e.com", FullName = "U3", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var handler = new ToggleReactionCommandHandler(db, new FakeCurrentUserService(userId: 3), new SpyChatNotifier());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new ToggleReactionCommand(10, "❤️"), CancellationToken.None));
    }
}
