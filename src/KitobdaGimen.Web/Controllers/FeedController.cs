using KitobdaGimen.Application.Features.Posts.Queries.GetFeed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
public class FeedController : AppController
{
    /// <summary>Initial number of posts rendered with the page; the rest stream in on scroll.</summary>
    private const int FeedPageSize = 4;

    /// <summary>The main feed page (first <see cref="FeedPageSize"/> posts only — more load on scroll).</summary>
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var feed = await Mediator.Send(new GetFeedQuery { Search = q, Page = page, PageSize = FeedPageSize });
        ViewData["Search"] = q;
        return View(feed);
    }

    /// <summary>Returns the next page of post cards as an HTML fragment (infinite scroll).</summary>
    public async Task<IActionResult> Cards(string? q, int page = 2)
    {
        var feed = await Mediator.Send(new GetFeedQuery { Search = q, Page = page, PageSize = FeedPageSize });
        ViewData["ShowAuthorActions"] = true;
        return PartialView("_FeedCards", feed.Items);
    }
}
