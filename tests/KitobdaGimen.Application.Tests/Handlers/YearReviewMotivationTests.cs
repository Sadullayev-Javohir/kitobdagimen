using KitobdaGimen.Application.Common;

namespace KitobdaGimen.Application.Tests.Handlers;

/// <summary>
/// "Yillik Kitob Yakuni" motivatsiya generatori va sana oynasi uchun testlar. Asosiy talab:
/// har bir foydalanuvchi uchun noyob (bir-biriga o'xshamaydigan) motivatsiya + determinizm.
/// </summary>
public class YearReviewMotivationTests
{
    [Fact]
    public void Motivation_is_deterministic_for_same_user_and_year()
    {
        var a = YearReviewMotivation.For(42, 2026, 5, 1200, 30);
        var b = YearReviewMotivation.For(42, 2026, 5, 1200, 30);

        Assert.Equal(a.Message, b.Message);
        Assert.Equal(a.ThemeVariant, b.ThemeVariant);
    }

    [Fact]
    public void Motivation_is_unique_across_1000_users()
    {
        var messages = new HashSet<string>();
        for (var uid = 1; uid <= 1000; uid++)
        {
            var r = YearReviewMotivation.For(uid, 2026, uid % 20, uid * 3, uid % 30);
            Assert.True(messages.Add(r.Message),
                $"Foydalanuvchi {uid} uchun motivatsiya takrorlandi: {r.Message}");
        }

        Assert.Equal(1000, messages.Count);
    }

    [Fact]
    public void ThemeVariant_is_within_range()
    {
        for (var uid = 1; uid <= 500; uid++)
        {
            var r = YearReviewMotivation.For(uid, 2026, 3, 100, 10);
            Assert.InRange(r.ThemeVariant, 0, YearReviewMotivation.ThemeCount - 1);
            Assert.NotEmpty(r.Emojis);
            Assert.False(string.IsNullOrWhiteSpace(r.PrimaryEmoji));
        }
    }

    [Fact]
    public void Message_includes_reading_stats_when_present()
    {
        var r = YearReviewMotivation.For(7, 2026, 12, 3400, 250);
        Assert.Contains("12", r.Message);
        Assert.Contains("3400", r.Message);
    }

    [Theory]
    [InlineData(2026, 12, 20, true)]   // oyna boshi
    [InlineData(2026, 12, 25, true)]   // oyna ichida
    [InlineData(2026, 12, 31, true)]   // dekabr oxiri (kiradi)
    [InlineData(2027, 1, 1, true)]     // 1-yanvar (kiradi)
    [InlineData(2026, 12, 19, false)]  // bir kun oldin
    [InlineData(2027, 1, 2, false)]    // 2-yanvar (kirmaydi)
    [InlineData(2026, 11, 25, false)]  // boshqa oy
    [InlineData(2026, 6, 20, false)]   // iyun
    public void Window_opens_between_dec_20_and_jan_1(int year, int month, int day, bool expected)
    {
        var date = new DateOnly(year, month, day);
        Assert.Equal(expected, YearReviewCalendar.IsWindowOpen(date));
    }

    [Fact]
    public void Window_is_thirteen_days()
    {
        Assert.Equal(13, YearReviewCalendar.WindowDays);
    }

    [Theory]
    [InlineData(2026, 12, 20, 2026)]   // dekabr -> shu yil
    [InlineData(2026, 12, 31, 2026)]   // dekabr -> shu yil
    [InlineData(2027, 1, 1, 2026)]     // 1-yanvar -> endigina tugagan (oldingi) yil
    public void ReportYear_is_the_year_being_wrapped_up(int year, int month, int day, int expected)
    {
        var date = new DateOnly(year, month, day);
        Assert.Equal(expected, YearReviewCalendar.ReportYear(date));
    }
}
