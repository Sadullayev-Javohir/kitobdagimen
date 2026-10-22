using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleQuoteLike;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class ToggleQuoteLikeCommandHandlerTests : TestBase
{
    private static async Task SeedQuoteAsync(TestDbContext db)
    {
        db.Users.Add(new User { Id = 1, GoogleId = "g-1", Email = "u1@e.com", FullName = "U1", CreatedAt = DateTime.UtcNow });
        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100 });
        db.Quotes.Add(new Quote { Id = 1, UserId = 1, BookId = 1, Slug = "slug1", Text = "Iqtibos", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Like_then_unlike_toggles_and_syncs_telegram()
    {
        using var db = CreateContext();
        await SeedQuoteAsync(db);
        db.Users.Add(new User { Id = 2, GoogleId = "g-2", Email = "u2@e.com", FullName = "U2", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var tgFake = new FakeTelegramBotService();
        var handler = new ToggleQuoteLikeCommandHandler(db, new FakeCurrentUserService(userId: 2), new SpyNotificationService(), tgFake);

        var liked = await handler.Handle(new ToggleQuoteLikeCommand(1), CancellationToken.None);
        Assert.True(liked.IsLiked);
        Assert.Equal(1, liked.LikeCount);
        Assert.Single(tgFake.SyncedQuoteLikes);
        Assert.Equal((1, 1), tgFake.SyncedQuoteLikes[0]);

        var unliked = await handler.Handle(new ToggleQuoteLikeCommand(1), CancellationToken.None);
        Assert.False(unliked.IsLiked);
        Assert.Equal(0, unliked.LikeCount);
        Assert.Equal(2, tgFake.SyncedQuoteLikes.Count);
        Assert.Equal((1, 0), tgFake.SyncedQuoteLikes[1]);
    }

    [Fact]
    public async Task Throws_when_quote_missing()
    {
        using var db = CreateContext();
        await SeedQuoteAsync(db);
        var handler = new ToggleQuoteLikeCommandHandler(db, new FakeCurrentUserService(userId: 2), new SpyNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ToggleQuoteLikeCommand(999), CancellationToken.None));
    }
}
