using KitobdaGimen.Application.Common;

namespace KitobdaGimen.Application.Tests;

public class UzTimeTests
{
    [Fact]
    public void FormatPublishedAt_ShowsJustNow_WhenLessThanOneMinute()
    {
        var utc = DateTime.UtcNow.AddSeconds(-30);
        var formatted = UzTime.FormatPublishedAt(utc);

        Assert.Equal("hozirgina", formatted);
    }

    [Fact]
    public void FormatPublishedAt_ShowsMinutesAgo_WhenLessThanOneHour()
    {
        var utc = DateTime.UtcNow.AddMinutes(-25);
        var formatted = UzTime.FormatPublishedAt(utc);

        Assert.Equal("25 daqiqa oldin", formatted);
    }

    [Fact]
    public void FormatPublishedAt_ShowsHoursAgo_WhenWithin24Hours()
    {
        // 4 soat oldin
        var utc4h = DateTime.UtcNow.AddHours(-4);
        Assert.Equal("4 soat oldin", UzTime.FormatPublishedAt(utc4h));

        // 23 soat oldin
        var utc23h = DateTime.UtcNow.AddHours(-23);
        Assert.Equal("23 soat oldin", UzTime.FormatPublishedAt(utc23h));
    }

    [Fact]
    public void FormatPublishedAt_ShowsFormattedDate_WhenOlderThan24Hours()
    {
        // 24 soatdan o'tganda dd.MM.yy formatida ko'rsatilishi kerak (masalan 02.10.26)
        // 2026-10-02 07:00:00 UTC (+5 = 12:00:00 Tashkent time)
        var utc = new DateTime(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc);
        var formatted = UzTime.FormatPublishedAt(utc);

        Assert.Equal("02.10.26", formatted);
    }

    [Fact]
    public void FormatExactPublishedAt_MatchesExactHourAndDateFormat()
    {
        // 12:43 UTC + 5 hours = 17:43 Tashkent time on 02.11.2026
        var utc = new DateTime(2026, 11, 2, 12, 43, 0, DateTimeKind.Utc);
        var formatted = UzTime.FormatExactPublishedAt(utc);

        Assert.Equal("17:43  02.11.26", formatted);
    }
}
