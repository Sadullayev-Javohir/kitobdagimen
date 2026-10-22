using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Features.Books.Queries.GetBooks;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KitobdaGimen.Application.Tests.Handlers;

public class BookSearchApostropheTests
{
    [Theory]
    [InlineData("O'tkan")]   // ASCII '
    [InlineData("O‘tkan")]   // Left curly ‘
    [InlineData("O’tkan")]   // Right curly ’
    [InlineData("Oʻtkan")]   // Modifier turned comma ʻ
    [InlineData("Oʼtkan")]   // Modifier apostrophe ʼ
    [InlineData("O`tkan")]   // Grave accent `
    [InlineData("O´tkan")]   // Acute accent ´
    [InlineData("Oʹtkan")]   // Prime ʹ
    public async Task Search_Finds_Book_With_Any_Apostrophe_Type(string searchQuery)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        // Kitob turli xil apostroflar bilan saqlangan bo'lishi mumkin
        db.Books.Add(new Book { Id = 1, Title = "Oʻtkan kunlar", Author = "Abdulla Qodiriy", TotalPages = 300 });
        db.Books.Add(new Book { Id = 2, Title = "O'tkan kunlar", Author = "Abdulla Qodiriy", TotalPages = 300 });
        db.Books.Add(new Book { Id = 3, Title = "O‘tkan kunlar", Author = "Abdulla Qodiriy", TotalPages = 300 });
        await db.SaveChangesAsync();

        var handler = new GetBooksQueryHandler(db);
        var result = await handler.Handle(new GetBooksQuery { Search = searchQuery }, CancellationToken.None);

        Assert.Equal(3, result.Count);
    }
}

