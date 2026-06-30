using KitobdaGimen.Application.Features.Leaderboard.Queries.GetReadingLeaderboard;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Tests.Handlers;

public class GetReadingLeaderboardQueryHandlerTests : TestBase
{
    /// <summary>"Bugun" handler bilan bir xil (O'zbekiston, UTC+5) hisoblanadi.</summary>
    private static DateOnly UzToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));

    private static void SeedUser(TestDbContext db, int userId)
    {
        db.Users.Add(new User
        {
            Id = userId,
            GoogleId = $"g-{userId}",
            Email = $"u{userId}@e.com",
            FullName = $"User {userId}",
            Username = $"user{userId}",
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>Bitta kitob bo'yicha maqsad + bir kunlik o'qish yozuvi qo'shadi.</summary>
    private static void SeedReading(TestDbContext db, int goalId, int userId, int bookId, DateOnly date, int pages)
    {
        if (!db.Books.Local.Any(b => b.Id == bookId))
        {
            db.Books.Add(new Book { Id = bookId, Title = $"Book {bookId}", Author = "A", TotalPages = 1000 });
        }

        db.ReadingGoals.Add(new ReadingGoal
        {
            Id = goalId, UserId = userId, BookId = bookId,
            DailyPageGoal = 10, StartDate = DateTime.UtcNow, CurrentPage = pages, IsActive = true
        });
        db.ReadingProgress.Add(new ReadingProgress
        {
            Id = goalId, ReadingGoalId = goalId, Date = date, PagesReadToday = pages
        });
    }

    [Fact]
    public async Task Ranks_users_by_pages_read_descending()
    {
        using var db = CreateContext();
        var today = UzToday();
        SeedUser(db, 1); SeedUser(db, 2); SeedUser(db, 3);
        SeedReading(db, goalId: 1, userId: 1, bookId: 1, date: today, pages: 50);
        SeedReading(db, goalId: 2, userId: 2, bookId: 2, date: today, pages: 120);
        SeedReading(db, goalId: 3, userId: 3, bookId: 3, date: today, pages: 80);
        await db.SaveChangesAsync();

        var handler = new GetReadingLeaderboardQueryHandler(db);
        var result = await handler.Handle(
            new GetReadingLeaderboardQuery { Period = LeaderboardPeriod.Daily, Limit = 23 }, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].UserId);   // 120 bet -> 1-o'rin
        Assert.Equal(1, result[0].Rank);
        Assert.Equal(120, result[0].Score);
        Assert.Equal(3, result[1].UserId);   // 80 bet -> 2-o'rin
        Assert.Equal(2, result[1].Rank);
        Assert.Equal(1, result[2].UserId);   // 50 bet -> 3-o'rin
        Assert.Equal(3, result[2].Rank);
    }

    [Fact]
    public async Task Sums_pages_across_books_and_counts_distinct_books()
    {
        using var db = CreateContext();
        var today = UzToday();
        SeedUser(db, 1);
        SeedReading(db, goalId: 1, userId: 1, bookId: 1, date: today, pages: 30);
        SeedReading(db, goalId: 2, userId: 1, bookId: 2, date: today, pages: 40);
        await db.SaveChangesAsync();

        var handler = new GetReadingLeaderboardQueryHandler(db);
        var result = await handler.Handle(
            new GetReadingLeaderboardQuery { Period = LeaderboardPeriod.Daily, Limit = 23 }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(70, result[0].Score);       // 30 + 40
        Assert.Equal("2 kitob", result[0].Detail); // 2 ta alohida kitob
    }

    [Fact]
    public async Task Daily_period_excludes_older_progress()
    {
        using var db = CreateContext();
        var today = UzToday();
        SeedUser(db, 1); SeedUser(db, 2);
        SeedReading(db, goalId: 1, userId: 1, bookId: 1, date: today, pages: 10);
        SeedReading(db, goalId: 2, userId: 2, bookId: 2, date: today.AddDays(-3), pages: 999); // 3 kun oldin
        await db.SaveChangesAsync();

        var handler = new GetReadingLeaderboardQueryHandler(db);
        var result = await handler.Handle(
            new GetReadingLeaderboardQuery { Period = LeaderboardPeriod.Daily, Limit = 23 }, CancellationToken.None);

        Assert.Single(result);          // faqat bugun o'qigan user 1
        Assert.Equal(1, result[0].UserId);
    }

    [Fact]
    public async Task Weekly_period_includes_last_seven_days()
    {
        using var db = CreateContext();
        var today = UzToday();
        SeedUser(db, 1);
        SeedReading(db, goalId: 1, userId: 1, bookId: 1, date: today.AddDays(-3), pages: 25);
        await db.SaveChangesAsync();

        var handler = new GetReadingLeaderboardQueryHandler(db);
        var result = await handler.Handle(
            new GetReadingLeaderboardQuery { Period = LeaderboardPeriod.Weekly, Limit = 23 }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(25, result[0].Score);
    }

    [Fact]
    public async Task Limit_caps_the_number_of_returned_users()
    {
        using var db = CreateContext();
        var today = UzToday();
        for (var i = 1; i <= 30; i++)
        {
            SeedUser(db, i);
            SeedReading(db, goalId: i, userId: i, bookId: i, date: today, pages: i);
        }
        await db.SaveChangesAsync();

        var handler = new GetReadingLeaderboardQueryHandler(db);
        var result = await handler.Handle(
            new GetReadingLeaderboardQuery { Period = LeaderboardPeriod.Daily, Limit = 23 }, CancellationToken.None);

        Assert.Equal(23, result.Count);          // top 23 only
        Assert.Equal(30, result[0].UserId);      // highest pages first
        Assert.Equal(8, result[22].UserId);      // 30th..8th = 23 users
    }

    [Fact]
    public async Task Returns_empty_when_no_reading()
    {
        using var db = CreateContext();
        SeedUser(db, 1);
        await db.SaveChangesAsync();

        var handler = new GetReadingLeaderboardQueryHandler(db);
        var result = await handler.Handle(
            new GetReadingLeaderboardQuery { Period = LeaderboardPeriod.AllTime, Limit = 23 }, CancellationToken.None);

        Assert.Empty(result);
    }
}
