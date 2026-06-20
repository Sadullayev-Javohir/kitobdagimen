using KitobdaGimen.Application.Features.Profile.Queries.GetUserByUsername;
using KitobdaGimen.Application.Features.Profile.Queries.GetUserPosts;
using KitobdaGimen.Application.Features.Profile.Queries.GetUserProfile;
using KitobdaGimen.Application.Features.Quotes.Queries.GetUserQuotes;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetActiveReadingGoals;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetFinishedReadingGoals;
using KitobdaGimen.Application.Features.Stories.Queries.GetUserStoryHistory;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Public, shareable profile pages at <c>/u/{id}</c>. Unlike <see cref="ProfileController"/>
/// these are anonymous-friendly so a link can be shared with people who have not registered yet.
/// The same <c>Profile/Index</c> view is reused; owner-only controls hide automatically because
/// an anonymous visitor is never the profile owner.
/// </summary>
[AllowAnonymous]
[Route("u")]
public class PublicProfileController : AppController
{
    /// <summary>Shareable, unique-username URL: <c>/u/{username}</c>. Resolves to the user id, then renders.</summary>
    [HttpGet("{username}")]
    public async Task<IActionResult> ByUsername(string username, int page = 1)
    {
        var id = await Mediator.Send(new GetUserIdByUsernameQuery(username));
        return await Index(id, page);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Index(int id, int page = 1)
    {
        // A logged-in user reaching their own public link is sent to the full /profile experience.
        if (CurrentUserId == id)
        {
            return RedirectToAction("Index", "Profile", new { id, page });
        }

        var profile = await Mediator.Send(new GetUserProfileQuery(id));
        var posts = await Mediator.Send(new GetUserPostsQuery { UserId = id, Page = page });
        var finishedBooks = await Mediator.Send(new GetFinishedReadingGoalsQuery(id));
        var activeBooks = await Mediator.Send(new GetActiveReadingGoalsQuery(id));
        var stories = await Mediator.Send(new GetUserStoryHistoryQuery(id));
        var quotes = (await Mediator.Send(new GetUserQuotesQuery(id) { PageSize = 50 })).Items;

        return View("~/Views/Profile/Index.cshtml", new ProfilePageViewModel
        {
            Profile = profile,
            Posts = posts,
            FinishedBooks = finishedBooks,
            CurrentBooks = activeBooks,
            Stories = stories,
            MyQuotes = quotes
        });
    }
}
