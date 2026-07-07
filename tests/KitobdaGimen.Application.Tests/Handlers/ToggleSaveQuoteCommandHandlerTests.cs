using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleSaveQuote;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class ToggleSaveQuoteCommandHandlerTests : TestBase
{
    private static async Task SeedQuoteAsync(TestDbContext db)
    {
        db.Users.Add(new User { Id = 1, GoogleId = "g-1", Email = "u1@e.com", FullName = "U1", CreatedAt = DateTime.UtcNow });
        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100 });
        db.Quotes.Add(new Quote { Id = 1, UserId = 1, BookId = 1, Slug = "slug1", Text = "Iqtibos", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Save_then_unsave_toggles_state_and_count()
    {
        using var db = CreateContext();
        await SeedQuoteAsync(db);
        var handler = new ToggleSaveQuoteCommandHandler(db, new FakeCurrentUserService(userId: 2));

        var saved = await handler.Handle(new ToggleSaveQuoteCommand(1), CancellationToken.None);
        Assert.True(saved.IsSaved);
        Assert.Equal(1, saved.SaveCount);
        Assert.Single(await db.SavedQuotes.ToListAsync());

        var unsaved = await handler.Handle(new ToggleSaveQuoteCommand(1), CancellationToken.None);
        Assert.False(unsaved.IsSaved);
        Assert.Equal(0, unsaved.SaveCount);
        Assert.Empty(await db.SavedQuotes.ToListAsync());
    }

    [Fact]
    public async Task Throws_when_quote_missing()
    {
        using var db = CreateContext();
        await SeedQuoteAsync(db);
        var handler = new ToggleSaveQuoteCommandHandler(db, new FakeCurrentUserService(userId: 2));

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ToggleSaveQuoteCommand(999), CancellationToken.None));
    }
}
