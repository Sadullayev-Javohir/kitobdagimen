using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.ReadingGoals.Commands.DeleteProgressEntry;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class DeleteProgressEntryCommandHandlerTests : TestBase
{
    private static async Task SeedFinishedGoalAsync(TestDbContext db, int userId = 1, int totalPages = 300)
    {
        db.Users.Add(new User { Id = userId, GoogleId = $"g-{userId}", Email = $"u{userId}@e.com", FullName = $"U{userId}", CreatedAt = DateTime.UtcNow });
        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = totalPages });
        // Foydalanuvchi avval 10 bet o'qigan, keyin xato bilan 290 bet kiritgan — kitob "tugatilgan"ga o'tgan.
        db.ReadingGoals.Add(new ReadingGoal { Id = 1, UserId = userId, BookId = 1, DailyPageGoal = 20, StartDate = DateTime.UtcNow, CurrentPage = totalPages, IsActive = false });
        db.ReadingProgress.Add(new ReadingProgress { Id = 1, ReadingGoalId = 1, Date = KitobdaGimen.Application.Common.UzTime.Today.AddDays(-3), PagesReadToday = 10 });
        db.ReadingProgress.Add(new ReadingProgress { Id = 2, ReadingGoalId = 1, Date = KitobdaGimen.Application.Common.UzTime.Today, PagesReadToday = 290 });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Deleting_mistaken_entry_reactivates_book_and_recomputes_page()
    {
        using var db = CreateContext();
        await SeedFinishedGoalAsync(db);
        var handler = new DeleteProgressEntryCommandHandler(db, new FakeCurrentUserService(userId: 1));

        var dto = await handler.Handle(
            new DeleteProgressEntryCommand { ReadingGoalId = 1, Date = KitobdaGimen.Application.Common.UzTime.Today },
            CancellationToken.None);

        Assert.Equal(10, dto.CurrentPage);   // faqat avvalgi 10 bet qoladi
        Assert.True(dto.IsActive);           // kitob yana faol
        Assert.Equal(0, dto.PagesReadToday); // bugungi yozuv o'chirildi
        Assert.Single(await db.ReadingProgress.ToListAsync()); // avvalgi kun saqlanib qoldi
    }

    [Fact]
    public async Task Deleting_only_entry_resets_page_to_zero()
    {
        using var db = CreateContext();
        await SeedFinishedGoalAsync(db);
        var handler = new DeleteProgressEntryCommandHandler(db, new FakeCurrentUserService(userId: 1));

        await handler.Handle(new DeleteProgressEntryCommand { ReadingGoalId = 1, Date = KitobdaGimen.Application.Common.UzTime.Today }, CancellationToken.None);
        var dto = await handler.Handle(new DeleteProgressEntryCommand { ReadingGoalId = 1, Date = KitobdaGimen.Application.Common.UzTime.Today.AddDays(-3) }, CancellationToken.None);

        Assert.Equal(0, dto.CurrentPage);
        Assert.True(dto.IsActive);
        Assert.Empty(await db.ReadingProgress.ToListAsync());
    }

    [Fact]
    public async Task Cannot_delete_another_users_entry()
    {
        using var db = CreateContext();
        await SeedFinishedGoalAsync(db, userId: 1);
        var handler = new DeleteProgressEntryCommandHandler(db, new FakeCurrentUserService(userId: 2));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new DeleteProgressEntryCommand { ReadingGoalId = 1, Date = KitobdaGimen.Application.Common.UzTime.Today }, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_entry_missing_for_date()
    {
        using var db = CreateContext();
        await SeedFinishedGoalAsync(db, userId: 1);
        var handler = new DeleteProgressEntryCommandHandler(db, new FakeCurrentUserService(userId: 1));

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteProgressEntryCommand { ReadingGoalId = 1, Date = KitobdaGimen.Application.Common.UzTime.Today.AddDays(-99) }, CancellationToken.None));
    }
}
