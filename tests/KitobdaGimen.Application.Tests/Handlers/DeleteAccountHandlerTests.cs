using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Profile.Commands.DeleteAccount;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = KitobdaGimen.Application.Common.Exceptions.ValidationException;

namespace KitobdaGimen.Application.Tests.Handlers;

public class DeleteAccountHandlerTests : TestBase
{
    private static async Task SeedUsersAsync(TestDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }

    // Only a super admin may delete an account, so promote the acting user before deletion.
    private static async Task MakeSuperAdminAsync(TestDbContext db, int userId)
    {
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.Role = UserRole.SuperAdmin;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Deletes_user_and_removes_connections_in_both_directions()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2, 3);
        db.Connections.Add(new Connection { RequesterId = 1, AddresseeId = 2, Status = ConnectionStatus.Accepted, CreatedAt = DateTime.UtcNow });
        db.Connections.Add(new Connection { RequesterId = 3, AddresseeId = 1, Status = ConnectionStatus.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await MakeSuperAdminAsync(db, 1);

        var handler = new DeleteAccountCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await handler.Handle(new DeleteAccountCommand("u1@e.com"), CancellationToken.None);

        Assert.Null(await db.Users.FirstOrDefaultAsync(u => u.Id == 1));
        Assert.Empty(await db.Connections.ToListAsync()); // both connections involving user 1 gone
        Assert.Equal(2, await db.Users.CountAsync()); // users 2 and 3 remain
    }

    [Fact]
    public async Task Wrong_email_does_not_delete()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1);
        await MakeSuperAdminAsync(db, 1);
        var handler = new DeleteAccountCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new DeleteAccountCommand("wrong@e.com"), CancellationToken.None));

        Assert.NotNull(await db.Users.FirstOrDefaultAsync(u => u.Id == 1));
    }

    [Fact]
    public async Task Non_super_admin_cannot_delete_account()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1); // default role is User

        var handler = new DeleteAccountCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new DeleteAccountCommand("u1@e.com"), CancellationToken.None));

        Assert.NotNull(await db.Users.FirstOrDefaultAsync(u => u.Id == 1));
    }

    [Fact]
    public async Task Removes_quote_challenge_and_message_engagement_by_the_user()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2, 3);

        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100, GenreId = 1 });
        // Quote owned by user 2; user 1 likes and comments on it.
        db.Quotes.Add(new Quote { Id = 1, UserId = 2, BookId = 1, Text = "q", CreatedAt = DateTime.UtcNow });
        db.QuoteLikes.Add(new QuoteLike { Id = 1, QuoteId = 1, UserId = 1, CreatedAt = DateTime.UtcNow });
        db.QuoteComments.Add(new QuoteComment { Id = 1, QuoteId = 1, UserId = 1, Text = "c", CreatedAt = DateTime.UtcNow });

        // Challenge winner is user 2; user 1 likes the winner row.
        db.ChallengeWinners.Add(new ChallengeWinner
        {
            Id = 1, Year = 2026, Month = 7, UserId = 2, Rank = 1,
            PagesRead = 100, BooksRead = 2, AnnouncedAt = DateTime.UtcNow
        });
        db.ChallengeWinnerLikes.Add(new ChallengeWinnerLike { Id = 1, ChallengeWinnerId = 1, UserId = 1, CreatedAt = DateTime.UtcNow });

        // Message in a conversation between users 2 and 3; user 1 reacted to it.
        db.Conversations.Add(new Conversation { Id = 1, User1Id = 2, User2Id = 3, CreatedAt = DateTime.UtcNow });
        db.Messages.Add(new Message { Id = 1, ConversationId = 1, SenderId = 2, Text = "hi", SentAt = DateTime.UtcNow });
        db.MessageReactions.Add(new MessageReaction { Id = 1, MessageId = 1, UserId = 1, Emoji = "❤️", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await MakeSuperAdminAsync(db, 1);

        var handler = new DeleteAccountCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await handler.Handle(new DeleteAccountCommand("u1@e.com"), CancellationToken.None);

        Assert.Null(await db.Users.FirstOrDefaultAsync(u => u.Id == 1));
        Assert.Empty(await db.QuoteLikes.Where(x => x.UserId == 1).ToListAsync());
        Assert.Empty(await db.QuoteComments.Where(x => x.UserId == 1).ToListAsync());
        Assert.Empty(await db.ChallengeWinnerLikes.Where(x => x.UserId == 1).ToListAsync());
        Assert.Empty(await db.MessageReactions.Where(x => x.UserId == 1).ToListAsync());
    }
}
