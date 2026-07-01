using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Admin.Analytics;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Application.Tests.Handlers;

public class GetFounderAnalyticsQueryHandlerTests : TestBase
{
    /// <summary>Same day boundary the handler uses (Uzbekistan, UTC+5).</summary>
    private static DateTime UtcForLocalDaysAgo(int days) => DateTime.UtcNow.AddDays(-days);

    private static User SeedUser(TestDbContext db, int id, UserRole role = UserRole.User, int createdDaysAgo = 0)
    {
        var u = new User
        {
            Id = id,
            GoogleId = $"g-{id}",
            Email = $"u{id}@e.com",
            FullName = $"User {id}",
            Username = $"user{id}",
            Role = role,
            CreatedAt = UtcForLocalDaysAgo(createdDaysAgo)
        };
        db.Users.Add(u);
        return u;
    }

    private static void SeedPost(TestDbContext db, int id, int userId, int createdDaysAgo)
    {
        if (!db.Books.Local.Any(b => b.Id == 1))
        {
            db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100 });
        }
        db.Posts.Add(new Post
        {
            Id = id,
            UserId = userId,
            BookId = 1,
            Slug = $"slug-{id}",
            ReviewText = "text",
            CreatedAt = UtcForLocalDaysAgo(createdDaysAgo)
        });
    }

    private static GetFounderAnalyticsQueryHandler Handler(TestDbContext db, int callerId) =>
        new(db, new FakeCurrentUserService(userId: callerId));

    [Fact]
    public async Task Throws_for_non_super_admin()
    {
        using var db = CreateContext();
        SeedUser(db, 1, UserRole.Admin);
        await db.SaveChangesAsync();

        var handler = Handler(db, callerId: 1);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new GetFounderAnalyticsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_for_anonymous()
    {
        using var db = CreateContext();
        await db.SaveChangesAsync();

        var handler = new GetFounderAnalyticsQueryHandler(db, new FakeCurrentUserService(userId: null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new GetFounderAnalyticsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Computes_totals_and_signup_counts()
    {
        using var db = CreateContext();
        SeedUser(db, 1, UserRole.SuperAdmin, createdDaysAgo: 0);  // today
        SeedUser(db, 2, createdDaysAgo: 3);                       // within 7d
        SeedUser(db, 3, createdDaysAgo: 20);                      // within 30d
        SeedUser(db, 4, createdDaysAgo: 100);                     // old
        SeedPost(db, 1, userId: 2, createdDaysAgo: 3);
        await db.SaveChangesAsync();

        var result = await Handler(db, callerId: 1).Handle(new GetFounderAnalyticsQuery(), CancellationToken.None);

        Assert.Equal(4, result.TotalUsers);
        Assert.Equal(1, result.TotalPosts);
        Assert.Equal(1, result.NewUsersToday);  // user 1
        Assert.Equal(2, result.NewUsers7d);     // users 1 & 2
        Assert.Equal(3, result.NewUsers30d);    // users 1, 2, 3
    }

    [Fact]
    public async Task Dau_counts_distinct_active_users_today()
    {
        using var db = CreateContext();
        SeedUser(db, 1, UserRole.SuperAdmin, createdDaysAgo: 40);  // caller, old signup (not active today by signup)
        SeedUser(db, 2, createdDaysAgo: 40);
        SeedUser(db, 3, createdDaysAgo: 40);
        // user 2 posts twice today -> still counts once; user 3 posted 5 days ago (not today)
        SeedPost(db, 1, userId: 2, createdDaysAgo: 0);
        SeedPost(db, 2, userId: 2, createdDaysAgo: 0);
        SeedPost(db, 3, userId: 3, createdDaysAgo: 5);
        await db.SaveChangesAsync();

        var result = await Handler(db, callerId: 1).Handle(new GetFounderAnalyticsQuery(), CancellationToken.None);

        Assert.Equal(1, result.Dau);   // only user 2 active today
        Assert.Equal(2, result.Wau);   // user 2 (today) + user 3 (5 days ago) within 7d
    }

    [Fact]
    public async Task Builds_funnel_with_registration_onboarding_activation()
    {
        using var db = CreateContext();
        SeedUser(db, 1, UserRole.SuperAdmin, createdDaysAgo: 10);
        SeedUser(db, 2, createdDaysAgo: 10);
        SeedUser(db, 3, createdDaysAgo: 10);
        db.Genres.Add(new Genre { Id = 1, Name = "Roman" });
        // users 1 & 2 completed onboarding (picked a genre)
        db.UserGenres.Add(new UserGenre { UserId = 1, GenreId = 1 });
        db.UserGenres.Add(new UserGenre { UserId = 2, GenreId = 1 });
        // only user 2 created content (a post)
        SeedPost(db, 1, userId: 2, createdDaysAgo: 1);
        await db.SaveChangesAsync();

        var result = await Handler(db, callerId: 1).Handle(new GetFounderAnalyticsQuery(), CancellationToken.None);

        Assert.Equal(4, result.Funnel.Count);
        Assert.Equal("registered", result.Funnel[0].Key);
        Assert.Equal(3, result.Funnel[0].Count);   // total users
        Assert.Equal("onboarded", result.Funnel[1].Key);
        Assert.Equal(2, result.Funnel[1].Count);   // users 1 & 2
        Assert.Equal("activated", result.Funnel[2].Key);
        Assert.Equal(1, result.Funnel[2].Count);   // user 2 only
    }

    [Fact]
    public async Task Daily_activity_series_covers_thirty_days()
    {
        using var db = CreateContext();
        SeedUser(db, 1, UserRole.SuperAdmin, createdDaysAgo: 50);
        await db.SaveChangesAsync();

        var result = await Handler(db, callerId: 1).Handle(new GetFounderAnalyticsQuery(), CancellationToken.None);

        Assert.Equal(30, result.DailyActivity.Count);
        Assert.Equal(8, result.RetentionWeekCount);
        Assert.Equal(8, result.Retention.Count);
    }
}
