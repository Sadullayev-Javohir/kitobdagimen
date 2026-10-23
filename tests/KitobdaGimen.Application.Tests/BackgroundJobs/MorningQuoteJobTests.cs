using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KitobdaGimen.Application.Tests.BackgroundJobs;

public class MorningQuoteJobTests : TestBase
{
    [Fact]
    public async Task RunAsync_PicksFirstQuote_Broadcasts_AndDeletesItFromDatabase()
    {
        // Arrange
        using var db = CreateContext();
        var telegramBot = new FakeTelegramBotService();
        var logger = NullLogger<MorningQuoteJob>.Instance;

        var quote1 = new MorningQuote
        {
            Id = 1,
            OrderIndex = 1,
            Author = "Robin Sharma",
            Source = "The 5 AM Club",
            Text = "Tong soat 5:00 da uyg'oning..."
        };

        var quote2 = new MorningQuote
        {
            Id = 2,
            OrderIndex = 2,
            Author = "Markus Avreliy",
            Source = "O'ylar",
            Text = "Tongda o'rningizdan turing..."
        };

        db.MorningQuotes.AddRange(quote1, quote2);
        await db.SaveChangesAsync();

        var job = new MorningQuoteJob(db, telegramBot, logger);

        // Act - 1-marta ishga tushganda (birinchi kun 05:30)
        await job.RunAsync(CancellationToken.None);

        // Assert - birinchi iqtibos o'chirilgan, faqat 2-iqtibos qolgan bo'lishi kerak
        var remainingQuotes = db.MorningQuotes.ToList();
        Assert.Single(remainingQuotes);
        Assert.Equal(2, remainingQuotes[0].Id);
        Assert.Equal("Markus Avreliy", remainingQuotes[0].Author);

        // Act - 2-marta ishga tushganda (ikkinchi kun 05:30)
        await job.RunAsync(CancellationToken.None);

        // Assert - ikkinchi iqtibos ham o'chirilgan, baza bo'sh qolishi kerak
        Assert.Empty(db.MorningQuotes);
    }
}

