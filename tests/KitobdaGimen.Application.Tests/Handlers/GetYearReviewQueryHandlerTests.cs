using KitobdaGimen.Application.Features.YearReview.Queries.GetYearReview;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Tests.Handlers;

/// <summary>
/// "Yillik Kitob Yakuni" hisobotini yig'uvchi handler testlari: o'qilgan kitob/bet soni,
/// faol kunlar, eng ko'p like yig'gan post va iqtibos — hammasi berilgan yil ichida.
/// </summary>
public class GetYearReviewQueryHandlerTests : TestBase
{
    private const int Year = 2026;

    private static void SeedUser(TestDbContext db, int userId)
    {
        db.Users.Add(new User
        {
            Id = userId,
            GoogleId = $"g-{userId}",
            Email = $"u{userId}@e.com",
            FullName = $"Kitobxon {userId}",
            Username = $"user{userId}",
            CreatedAt = DateTime.UtcNow
        });
    }

    private static void SeedBook(TestDbContext db, int bookId, string title)
    {
        db.Books.Add(new Book { Id = bookId, Title = title, Author = "Muallif", TotalPages = 1000 });
    }

    private static void SeedReading(
        TestDbContext db, int id, int userId, int bookId, DateOnly date, int pages)
    {
        db.ReadingGoals.Add(new ReadingGoal
        {
            Id = id, UserId = userId, BookId = bookId,
            DailyPageGoal = 10, StartDate = DateTime.UtcNow, CurrentPage = pages, IsActive = true
        });
        db.ReadingProgress.Add(new ReadingProgress
        {
            Id = id, ReadingGoalId = id, Date = date, PagesReadToday = pages
        });
    }

    [Fact]
    public async Task Aggregates_books_pages_and_active_days_within_the_year()
    {
        using var db = CreateContext();
        SeedUser(db, 1);
        SeedBook(db, 10, "Sap-sariq devlar");
        SeedBook(db, 11, "O'tkan kunlar");

        // Yil ichida: 2 kitob, 2 kun, 50 + 70 + 30 = 150 bet
        SeedReading(db, 1, 1, 10, new DateOnly(Year, 3, 1), 50);
        SeedReading(db, 2, 1, 10, new DateOnly(Year, 3, 2), 70);
        SeedReading(db, 3, 1, 11, new DateOnly(Year, 3, 2), 30);
        // Boshqa yil — hisobga kirmaydi
        SeedReading(db, 4, 1, 11, new DateOnly(Year - 1, 12, 31), 999);
        await db.SaveChangesAsync();

        var handler = new GetYearReviewQueryHandler(db);
        var dto = await handler.Handle(new GetYearReviewQuery(1, Year), CancellationToken.None);

        Assert.Equal(2, dto.BooksRead);
        Assert.Equal(150, dto.TotalPages);
        Assert.Equal(2, dto.ActiveDays);
        Assert.Equal(2, dto.Books.Count);
        // Eng ko'p bet o'qilgan kitob birinchi bo'ladi (10-kitob: 120 bet).
        Assert.Equal("Sap-sariq devlar", dto.Books[0].Title);
        Assert.Equal(120, dto.Books[0].Pages);
        Assert.True(dto.HasActivity);
        Assert.False(string.IsNullOrWhiteSpace(dto.Motivation));
    }

    [Fact]
    public async Task Picks_the_most_liked_post_of_the_year()
    {
        using var db = CreateContext();
        SeedUser(db, 1);
        SeedUser(db, 2);
        SeedBook(db, 10, "Kitob A");

        var lowPost = new Post
        {
            Id = 1, UserId = 1, BookId = 10, Slug = "low", ReviewText = "<p>Kam like</p>",
            CreatedAt = new DateTime(Year, 5, 1)
        };
        var topPost = new Post
        {
            Id = 2, UserId = 1, BookId = 10, Slug = "top", ReviewText = "<b>Ko'p</b> like post",
            CreatedAt = new DateTime(Year, 6, 1)
        };
        db.Posts.AddRange(lowPost, topPost);
        db.Likes.Add(new Like { Id = 1, PostId = 1, UserId = 2, CreatedAt = DateTime.UtcNow });
        db.Likes.Add(new Like { Id = 2, PostId = 2, UserId = 1, CreatedAt = DateTime.UtcNow });
        db.Likes.Add(new Like { Id = 3, PostId = 2, UserId = 2, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new GetYearReviewQueryHandler(db);
        var dto = await handler.Handle(new GetYearReviewQuery(1, Year), CancellationToken.None);

        Assert.NotNull(dto.TopPost);
        Assert.Equal("top", dto.TopPost!.Slug);
        Assert.Equal(2, dto.TopPost.LikeCount);
        // HTML teglar olib tashlangan bo'lishi kerak.
        Assert.DoesNotContain("<", dto.TopPost.Snippet);
        Assert.Contains("like post", dto.TopPost.Snippet);
    }

    [Fact]
    public async Task Picks_the_most_liked_quote_of_the_year()
    {
        using var db = CreateContext();
        SeedUser(db, 1);
        SeedUser(db, 2);
        SeedBook(db, 10, "Kitob A");

        db.Quotes.Add(new Quote { Id = 1, UserId = 1, BookId = 10, Text = "Kam iqtibos", CreatedAt = new DateTime(Year, 2, 1) });
        db.Quotes.Add(new Quote { Id = 2, UserId = 1, BookId = 10, Text = "Sevimli iqtibos", CreatedAt = new DateTime(Year, 4, 1) });
        db.QuoteLikes.Add(new QuoteLike { Id = 1, QuoteId = 2, UserId = 1, CreatedAt = DateTime.UtcNow });
        db.QuoteLikes.Add(new QuoteLike { Id = 2, QuoteId = 2, UserId = 2, CreatedAt = DateTime.UtcNow });
        db.QuoteLikes.Add(new QuoteLike { Id = 3, QuoteId = 1, UserId = 2, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new GetYearReviewQueryHandler(db);
        var dto = await handler.Handle(new GetYearReviewQuery(1, Year), CancellationToken.None);

        Assert.NotNull(dto.TopQuote);
        Assert.Equal(2, dto.TopQuote!.Id);
        Assert.Equal(2, dto.TopQuote.LikeCount);
        Assert.Contains("Sevimli", dto.TopQuote.Snippet);
    }

    [Fact]
    public async Task Empty_user_gets_a_gentle_card_without_activity()
    {
        using var db = CreateContext();
        SeedUser(db, 1);
        await db.SaveChangesAsync();

        var handler = new GetYearReviewQueryHandler(db);
        var dto = await handler.Handle(new GetYearReviewQuery(1, Year), CancellationToken.None);

        Assert.Equal(0, dto.BooksRead);
        Assert.Equal(0, dto.TotalPages);
        Assert.Null(dto.TopPost);
        Assert.Null(dto.TopQuote);
        Assert.False(dto.HasActivity);
        Assert.False(string.IsNullOrWhiteSpace(dto.Motivation));
    }
}
