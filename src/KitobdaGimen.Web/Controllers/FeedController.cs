using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetLibrary;
using KitobdaGimen.Application.Features.Posts.Queries.GetFeed;
using KitobdaGimen.Application.Features.Profile.Dtos;
using KitobdaGimen.Application.Features.Profile.Queries.GetUserProfile;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetActiveReadingGoals;
using KitobdaGimen.Domain.Enums;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

public class FeedController : AppController
{
    private readonly IDailyJackpotService _jackpotService;

    public FeedController(IDailyJackpotService jackpotService)
    {
        _jackpotService = jackpotService;
    }

    /// <summary>Initial number of posts rendered with the page; the rest stream in on scroll.</summary>
    private const int FeedPageSize = 4;

    private async Task SetIsAdminAsync()
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        ViewData["IsAdmin"] = me != null && me.Role >= UserRole.Admin;
    }

    /// <summary>The main feed page (first <see cref="FeedPageSize"/> posts only — more load on scroll).</summary>
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var feed = await Mediator.Send(new GetFeedQuery { Search = q, Page = page, PageSize = FeedPageSize });
        var me = await Mediator.Send(new GetCurrentUserQuery());

        ProfileDto? userProfile = null;
        if (me != null)
        {
            try
            {
                userProfile = await Mediator.Send(new GetUserProfileQuery(me.Id));
            }
            catch { }
        }

        ReadingGoalDto? activeGoal = null;
        if (me != null)
        {
            try
            {
                var goals = await Mediator.Send(new GetActiveReadingGoalsQuery());
                activeGoal = goals?.FirstOrDefault();
            }
            catch { }
        }

        IReadOnlyList<ChallengeStandingDto> topReaders = Array.Empty<ChallengeStandingDto>();
        try
        {
            var now = UzTime.Now;
            topReaders = await Mediator.Send(new GetChallengeStandingsQuery { Year = now.Year, Month = now.Month, Limit = 3 });
        }
        catch { }

        IReadOnlyList<PhysicalBookDto> exchangeBooks = Array.Empty<PhysicalBookDto>();
        try
        {
            exchangeBooks = await Mediator.Send(new GetLibraryQuery { Limit = 3 });
        }
        catch { }

        KitobdaGimen.Application.Features.Jackpot.Dtos.DailyJackpotDto? dailyJackpot = null;
        try
        {
            dailyJackpot = await _jackpotService.GetTodayJackpotAsync(me?.Id);
        }
        catch { }

        var isAdmin = me != null && me.Role >= UserRole.Admin;
        ViewData["IsAdmin"] = isAdmin;
        ViewData["Search"] = q;
        ViewData["ShowAuthorActions"] = true;
        ViewData["TodayHeroUserId"] = dailyJackpot?.UserId;

        var vm = new FeedPageViewModel
        {
            Feed = feed,
            CurrentUser = me,
            UserProfile = userProfile,
            ActiveGoal = activeGoal,
            TopReaders = topReaders,
            ExchangeBooks = exchangeBooks,
            DailyJackpot = dailyJackpot,
            Search = q,
            IsAdmin = isAdmin
        };

        return View(vm);
    }

    /// <summary>Returns the next page of post cards as an HTML fragment (infinite scroll).</summary>
    public async Task<IActionResult> Cards(string? q, int page = 2)
    {
        var feed = await Mediator.Send(new GetFeedQuery { Search = q, Page = page, PageSize = FeedPageSize });
        ViewData["ShowAuthorActions"] = true;
        ViewData["TodayHeroUserId"] = await _jackpotService.GetTodayHeroUserIdAsync();
        await SetIsAdminAsync();
        return PartialView("_FeedCards", feed.Items);
    }
}
