using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;
using KitobdaGimen.Application.Features.Challenge.Commands.ToggleChallengeWinnerLike;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;
using KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeWins;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using MediatR;

namespace KitobdaGimen.Application.Tests.Handlers;

public class ChallengeHandlerTests : TestBase
{
    private static (int Year, int Month) CurrentPeriod() => ChallengeCalendar.CurrentPeriod();

    /// <summary>Davr ichidagi bir sana (bugun, O'zbekiston vaqti bilan).</summary>
    private static DateOnly InPeriodDate() => UzTime.Today;

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
    public async Task Standings_rank_users_by_pages_in_period()
    {
        using var db = CreateContext();
        var (y, m) = CurrentPeriod();
        var date = InPeriodDate();
        SeedUser(db, 1); SeedUser(db, 2); SeedUser(db, 3);
        SeedReading(db, 1, 1, 1, date, 50);
        SeedReading(db, 2, 2, 2, date, 120);
        SeedReading(db, 3, 3, 3, date, 80);
        await db.SaveChangesAsync();

        var handler = new GetChallengeStandingsQueryHandler(db, new FakeCurrentUserService());
        var result = await handler.Handle(
            new GetChallengeStandingsQuery { Year = y, Month = m, Limit = 3 }, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].UserId);   // 120 bet -> 1-o'rin
        Assert.Equal(1, result[0].Rank);
        Assert.Equal(120, result[0].PagesRead);
        Assert.Equal(3, result[1].UserId);   // 80 bet -> 2-o'rin
        Assert.Equal(1, result[2].UserId);   // 50 bet -> 3-o'rin
    }

    [Fact]
    public async Task Standings_exclude_reading_outside_the_period()
    {
        using var db = CreateContext();
        var (y, m) = CurrentPeriod();
        var date = InPeriodDate();
        SeedUser(db, 1); SeedUser(db, 2);
        SeedReading(db, 1, 1, 1, date, 40);
        SeedReading(db, 2, 2, 2, date.AddMonths(-6), 999); // davrdan tashqarida
        await db.SaveChangesAsync();

        var handler = new GetChallengeStandingsQueryHandler(db, new FakeCurrentUserService());
        var result = await handler.Handle(
            new GetChallengeStandingsQuery { Year = y, Month = m, Limit = 3 }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }

    [Fact]
    public async Task Finalize_persists_top_three_and_is_idempotent()
    {
        using var db = CreateContext();
        var (y, m) = CurrentPeriod();
        var date = InPeriodDate();
        for (var i = 1; i <= 4; i++)
        {
            SeedUser(db, i);
            SeedReading(db, i, i, i, date, i * 10); // 10,20,30,40
        }
        await db.SaveChangesAsync();

        var mediator = new StandingsOnlySender(db);
        var handler = new FinalizeChallengeMonthCommandHandler(db, new FakeCurrentUserService(), mediator);

        var count = await handler.Handle(
            new FinalizeChallengeMonthCommand(y, m) { BypassAdminCheck = true }, CancellationToken.None);

        Assert.Equal(3, count);
        var winners = db.ChallengeWinners.Where(w => w.Year == y && w.Month == m).OrderBy(w => w.Rank).ToList();
        Assert.Equal(3, winners.Count);
        Assert.Equal(4, winners[0].UserId); // 40 bet -> 1-o'rin
        Assert.Equal(40, winners[0].PagesRead);

        // Ikkinchi marta — idempotent (qayta yaratmaydi).
        var again = await handler.Handle(
            new FinalizeChallengeMonthCommand(y, m) { BypassAdminCheck = true }, CancellationToken.None);
        Assert.Equal(0, again);
        Assert.Equal(3, db.ChallengeWinners.Count(w => w.Year == y && w.Month == m));
    }

    [Fact]
    public async Task Toggle_like_adds_then_removes()
    {
        using var db = CreateContext();
        SeedUser(db, 1);
        db.ChallengeWinners.Add(new ChallengeWinner
        {
            Id = 100, Year = 2026, Month = 5, UserId = 1, Rank = 1,
            PagesRead = 100, BooksRead = 2, ActiveDays = 10, AvgPagesPerDay = 10, AnnouncedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new ToggleChallengeWinnerLikeCommandHandler(db, new FakeCurrentUserService(1));

        var liked = await handler.Handle(new ToggleChallengeWinnerLikeCommand(100), CancellationToken.None);
        Assert.True(liked.Liked);
        Assert.Equal(1, liked.LikeCount);

        var unliked = await handler.Handle(new ToggleChallengeWinnerLikeCommand(100), CancellationToken.None);
        Assert.False(unliked.Liked);
        Assert.Equal(0, unliked.LikeCount);
    }

    [Fact]
    public async Task User_wins_are_returned_newest_first()
    {
        using var db = CreateContext();
        SeedUser(db, 1);
        db.ChallengeWinners.Add(new ChallengeWinner
        {
            Id = 1, Year = 2026, Month = 1, UserId = 1, Rank = 2, PagesRead = 50, BooksRead = 1, AnnouncedAt = DateTime.UtcNow
        });
        db.ChallengeWinners.Add(new ChallengeWinner
        {
            Id = 2, Year = 2026, Month = 3, UserId = 1, Rank = 1, PagesRead = 90, BooksRead = 3,
            GiftBookTitle = "Atom odatlari", GiftBookCoverUrl = "/uploads/covers/x.webp", AnnouncedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetUserChallengeWinsQueryHandler(db);
        var wins = await handler.Handle(new GetUserChallengeWinsQuery(1), CancellationToken.None);

        Assert.Equal(2, wins.Count);
        Assert.Equal(3, wins[0].Month);            // eng yangi birinchi
        Assert.Equal(1, wins[0].Rank);
        Assert.Equal("Atom odatlari", wins[0].GiftBookTitle);
    }

    /// <summary>
    /// FinalizeChallengeMonthCommandHandler ISender orqali GetChallengeStandingsQuery yuboradi;
    /// bu fake faqat o'sha so'rovni haqiqiy handler'ga uzatadi.
    /// </summary>
    private sealed class StandingsOnlySender : ISender
    {
        private readonly TestDbContext _db;
        public StandingsOnlySender(TestDbContext db) => _db = db;

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetChallengeStandingsQuery q)
            {
                var handler = new GetChallengeStandingsQueryHandler(_db, new FakeCurrentUserService());
                var result = await handler.Handle(q, cancellationToken);
                return (TResponse)result;
            }
            throw new NotSupportedException($"Unexpected request: {request.GetType().Name}");
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
