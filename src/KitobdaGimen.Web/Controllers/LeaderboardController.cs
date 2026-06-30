using KitobdaGimen.Application.Features.Leaderboard.Queries.GetReadingLeaderboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Kitobxonlar reytingi (leaderboard). Eng ko'p kitob o'qigan foydalanuvchilarni
/// ko'rsatadi: 1-2-3 o'rin (sahna/podium) va undan keyingi 20 o'rin — jami top 23.
/// Davrlar: kunlik, haftalik, oylik va bir umrlik.
/// </summary>
[Authorize]
[Route("leaderboards")]
public class LeaderboardController : AppController
{
    /// <summary>Reytingda ko'rsatiladigan o'rinlar soni: 1-2-3 + yana 20 = 23.</summary>
    private const int TopCount = 23;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? period)
    {
        var selected = ParsePeriod(period);

        var users = await Mediator.Send(new GetReadingLeaderboardQuery
        {
            Period = selected,
            Limit = TopCount
        });

        ViewData["Period"] = selected;
        ViewData["Title"] = "Kitobxonlar reytingi";

        return View(users);
    }

    private static LeaderboardPeriod ParsePeriod(string? period) => period?.ToLowerInvariant() switch
    {
        "daily" or "kunlik" => LeaderboardPeriod.Daily,
        "weekly" or "haftalik" => LeaderboardPeriod.Weekly,
        "monthly" or "oylik" => LeaderboardPeriod.Monthly,
        "alltime" or "all-time" or "umrlik" => LeaderboardPeriod.AllTime,
        _ => LeaderboardPeriod.Daily // default: kunlik
    };
}
