using KitobdaGimen.Application.Features.Onboarding.Commands.SaveUserGenres;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class SaveUserGenresCommandHandlerTests : TestBase
{
    private static async Task SeedGenresAsync(TestDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Genres.Add(new Genre { Id = id, Name = $"Janr-{id}" });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Saves_only_existing_genres()
    {
        using var db = CreateContext();
        await SeedGenresAsync(db, 1, 2, 3);
        var handler = new SaveUserGenresCommandHandler(db, new FakeCurrentUserService(userId: 10));

        // 99 does not exist and must be ignored.
        await handler.Handle(new SaveUserGenresCommand { GenreIds = new[] { 1, 2, 99 } }, CancellationToken.None);

        var saved = await db.UserGenres.Where(ug => ug.UserId == 10).Select(ug => ug.GenreId).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, saved.OrderBy(x => x));
    }

    [Fact]
    public async Task Replaces_previous_selection()
    {
        using var db = CreateContext();
        await SeedGenresAsync(db, 1, 2, 3);
        db.UserGenres.Add(new UserGenre { UserId = 10, GenreId = 1 });
        await db.SaveChangesAsync();

        var handler = new SaveUserGenresCommandHandler(db, new FakeCurrentUserService(userId: 10));
        await handler.Handle(new SaveUserGenresCommand { GenreIds = new[] { 2, 3 } }, CancellationToken.None);

        var saved = await db.UserGenres.Where(ug => ug.UserId == 10).Select(ug => ug.GenreId).ToListAsync();
        Assert.Equal(new[] { 2, 3 }, saved.OrderBy(x => x));
    }

    [Fact]
    public async Task Throws_when_not_authenticated()
    {
        using var db = CreateContext();
        var handler = new SaveUserGenresCommandHandler(db, new FakeCurrentUserService(userId: null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new SaveUserGenresCommand { GenreIds = new[] { 1 } }, CancellationToken.None));
    }
}
