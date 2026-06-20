using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Quotes.Commands.CreateQuote;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Tests.Handlers;

public class CreateQuoteHandlerTests : TestBase
{
    private static async Task SeedAuthorAndBookAsync(TestDbContext db, int userId = 1)
    {
        db.Users.Add(new User { Id = userId, GoogleId = $"g-{userId}", Email = $"u{userId}@e.com", FullName = $"User {userId}", CreatedAt = DateTime.UtcNow });
        db.Books.Add(new Book { Id = 1, Title = "O'tkan kunlar", Author = "Abdulla Qodiriy", TotalPages = 300 });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateQuote_persists_and_projects_quote()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db);
        var handler = new CreateQuoteCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        var dto = await handler.Handle(new CreateQuoteCommand { BookId = 1, Text = "Yaxshi gap" }, CancellationToken.None);

        Assert.Equal("Yaxshi gap", dto.Text);
        Assert.Equal(1, dto.Author.Id);
    }

    [Fact]
    public async Task CreateQuote_throws_when_book_missing()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db);
        var handler = new CreateQuoteCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new CreateQuoteCommand { BookId = 999, Text = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateQuote_notifies_each_follower()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db, userId: 1);
        db.Users.Add(new User { Id = 2, GoogleId = "g-2", Email = "u2@e.com", FullName = "User 2", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new User { Id = 3, GoogleId = "g-3", Email = "u3@e.com", FullName = "User 3", CreatedAt = DateTime.UtcNow });
        db.Follows.Add(new Follow { FollowerId = 2, FollowingId = 1, CreatedAt = DateTime.UtcNow });
        db.Follows.Add(new Follow { FollowerId = 3, FollowingId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var notifier = new SpyNotificationService();
        var handler = new CreateQuoteCommandHandler(db, new FakeCurrentUserService(userId: 1), notifier);

        await handler.Handle(new CreateQuoteCommand { BookId = 1, Text = "Iqtibos" }, CancellationToken.None);

        Assert.Equal(2, notifier.Sent.Count);
        Assert.Equal(new[] { 2, 3 }, notifier.Sent.Select(s => s.RecipientUserId).OrderBy(id => id).ToArray());
        Assert.All(notifier.Sent, s => Assert.Equal("quote", s.Notification.Type));
    }
}
