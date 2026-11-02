using KitobdaGimen.Application.Common;

namespace KitobdaGimen.Application.Tests;

public class UzTimeTests
{
    [Fact]
    public void FormatPublishedAt_MatchesUserRequestedFormat()
    {
        // 12:43 UTC + 5 hours = 17:43 Tashkent time on 02.11.2026
        var utc = new DateTime(2026, 11, 2, 12, 43, 0, DateTimeKind.Utc);
        var formatted = UzTime.FormatPublishedAt(utc);

        Assert.Equal("17:43  02.11.26", formatted);
    }

    [Fact]
    public void FormatPublishedAt_PadsZerosForSingleDigits()
    {
        // 04:05 UTC + 5 hours = 09:05 on 05.04.2025
        var utc = new DateTime(2025, 4, 5, 4, 5, 0, DateTimeKind.Utc);
        var formatted = UzTime.FormatPublishedAt(utc);

        Assert.Equal("09:05  05.04.25", formatted);
    }

    [Fact]
    public void FormatPublishedAt_HandlesUnspecifiedKind()
    {
        var unspecified = new DateTime(2026, 11, 2, 12, 43, 0, DateTimeKind.Unspecified);
        var formatted = UzTime.FormatPublishedAt(unspecified);

        Assert.Equal("17:43  02.11.26", formatted);
    }
}
