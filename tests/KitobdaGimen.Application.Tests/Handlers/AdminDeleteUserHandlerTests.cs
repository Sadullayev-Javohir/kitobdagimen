using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteUser;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class AdminDeleteUserHandlerTests : TestBase
{
    private static async Task SeedUsersAsync(TestDbContext db, params (int Id, UserRole Role)[] users)
    {
        foreach (var (id, role) in users)
        {
            db.Users.Add(new User
            {
                Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}",
                Role = role, CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Deletes_target_and_removes_their_restrict_engagement_rows()
    {
        using var db = CreateContext();
        // Caller (1) is SuperAdmin; target (2) placed engagement on content owned by user 3.
        await SeedUsersAsync(db, (1, UserRole.SuperAdmin), (2, UserRole.User), (3, UserRole.User));

        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100, GenreId = 1 });
        // Quote owned by user 3; target user 2 likes and comments on it (Restrict FKs to User).
        db.Quotes.Add(new Quote { Id = 1, UserId = 3, BookId = 1, Slug = "slug1", Text = "q", CreatedAt = DateTime.UtcNow });
        db.QuoteLikes.Add(new QuoteLike { Id = 1, QuoteId = 1, UserId = 2, CreatedAt = DateTime.UtcNow });
        db.QuoteComments.Add(new QuoteComment { Id = 1, QuoteId = 1, UserId = 2, Text = "c", CreatedAt = DateTime.UtcNow });

        // Challenge winner is user 3; target user 2 likes the winner row (Restrict FK).
        db.ChallengeWinners.Add(new ChallengeWinner
        {
            Id = 1, Year = 2026, Month = 7, UserId = 3, Rank = 1,
            PagesRead = 100, BooksRead = 2, AnnouncedAt = DateTime.UtcNow
        });
        db.ChallengeWinnerLikes.Add(new ChallengeWinnerLike { Id = 1, ChallengeWinnerId = 1, UserId = 2, CreatedAt = DateTime.UtcNow });

        // Message in a conversation between users 3 and 1; target user 2 reacted (Restrict FK).
        db.Conversations.Add(new Conversation { Id = 1, User1Id = 3, User2Id = 1, CreatedAt = DateTime.UtcNow });
        db.Messages.Add(new Message { Id = 1, ConversationId = 1, SenderId = 3, Text = "hi", SentAt = DateTime.UtcNow });
        db.MessageReactions.Add(new MessageReaction { Id = 1, MessageId = 1, UserId = 2, Emoji = "❤️", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new AdminDeleteUserCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await handler.Handle(new AdminDeleteUserCommand(2), CancellationToken.None);

        Assert.Null(await db.Users.FirstOrDefaultAsync(u => u.Id == 2));
        Assert.Empty(await db.QuoteLikes.Where(x => x.UserId == 2).ToListAsync());
        Assert.Empty(await db.QuoteComments.Where(x => x.UserId == 2).ToListAsync());
        Assert.Empty(await db.ChallengeWinnerLikes.Where(x => x.UserId == 2).ToListAsync());
        Assert.Empty(await db.MessageReactions.Where(x => x.UserId == 2).ToListAsync());
        // Bystander user 3's content is preserved.
        Assert.NotNull(await db.Users.FirstOrDefaultAsync(u => u.Id == 3));
        Assert.NotNull(await db.Quotes.FirstOrDefaultAsync(q => q.Id == 1));
    }

    [Fact]
    public async Task Cannot_delete_self()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, (1, UserRole.SuperAdmin));
        var handler = new AdminDeleteUserCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new AdminDeleteUserCommand(1), CancellationToken.None));

        Assert.NotNull(await db.Users.FirstOrDefaultAsync(u => u.Id == 1));
    }

    [Fact]
    public async Task Cannot_delete_super_admin()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, (1, UserRole.SuperAdmin), (2, UserRole.SuperAdmin));
        var handler = new AdminDeleteUserCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new AdminDeleteUserCommand(2), CancellationToken.None));

        Assert.NotNull(await db.Users.FirstOrDefaultAsync(u => u.Id == 2));
    }
}
