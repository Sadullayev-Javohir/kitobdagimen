using KitobdaGimen.Application.Features.Auth.Commands.LoginWithGoogle;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class LoginWithGoogleCommandHandlerTests : TestBase
{
    private static LoginWithGoogleCommand Command(string googleId = "g-123") => new()
    {
        GoogleId = googleId,
        Email = "ali@example.com",
        FullName = "Ali Valiyev",
        AvatarUrl = "https://example.com/a.png"
    };

    [Fact]
    public async Task Creates_new_user_and_requires_onboarding_on_first_login()
    {
        using var db = CreateContext();
        var tokens = new FakeTokenService();
        var handler = new LoginWithGoogleCommandHandler(db, tokens, CreateMapper());

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.Equal("test-token", result.Token);
        Assert.True(result.RequiresOnboarding); // new user has no genres
        Assert.Equal("ali@example.com", result.User.Email);

        var saved = await db.Users.SingleAsync();
        Assert.Equal("g-123", saved.GoogleId);
        Assert.Equal("Ali Valiyev", saved.FullName);
    }

    [Fact]
    public async Task Existing_user_with_genres_does_not_require_onboarding()
    {
        using var db = CreateContext();
        var user = new User { GoogleId = "g-123", Email = "old@example.com", FullName = "Eski Ism", CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.Genres.Add(new Genre { Id = 1, Name = "Roman" });
        await db.SaveChangesAsync();
        db.UserGenres.Add(new UserGenre { UserId = user.Id, GenreId = 1 });
        await db.SaveChangesAsync();

        var handler = new LoginWithGoogleCommandHandler(db, new FakeTokenService(), CreateMapper());
        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.False(result.RequiresOnboarding);
        Assert.Single(await db.Users.ToListAsync()); // no duplicate created
    }

    [Fact]
    public async Task Existing_user_profile_is_synced_from_google()
    {
        using var db = CreateContext();
        var user = new User { GoogleId = "g-123", Email = "old@example.com", FullName = "Eski Ism", CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new LoginWithGoogleCommandHandler(db, new FakeTokenService(), CreateMapper());
        await handler.Handle(Command(), CancellationToken.None);

        var updated = await db.Users.SingleAsync();
        Assert.Equal("ali@example.com", updated.Email);
        Assert.Equal("Ali Valiyev", updated.FullName);
        Assert.Equal("https://example.com/a.png", updated.AvatarUrl);
    }
}
