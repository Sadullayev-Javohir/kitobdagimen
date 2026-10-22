using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;
using KitobdaGimen.Application.Features.Quotes.Commands.AddQuoteComment;
using KitobdaGimen.Application.Features.Quotes.Commands.CreateQuote;
using KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuote;
using KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuoteComment;
using KitobdaGimen.Application.Features.Quotes.Commands.RecordQuoteView;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleQuoteLike;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleSaveQuote;
using KitobdaGimen.Application.Features.Quotes.Queries.GetAllQuotes;
using KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;
using KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteBySlug;
using KitobdaGimen.Domain.Enums;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("quotes")]
public class QuotesController : AppController
{
    private readonly KitobdaGimen.Application.Common.Interfaces.ITelegramBotService _telegramBot;

    public QuotesController(KitobdaGimen.Application.Common.Interfaces.ITelegramBotService telegramBot)
    {
        _telegramBot = telegramBot;
    }

    /// <summary>Exposes whether the current user is an admin so quote cards can show moderation actions.</summary>
    private async Task<bool> SetIsAdminAsync()
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        var isAdmin = me != null && me.Role >= UserRole.Admin;
        ViewData["IsAdmin"] = isAdmin;
        return isAdmin;
    }

    /// <summary>Sara iqtiboslar sahifasi — barcha kitobxonlarning iqtiboslari, qidiruv va saralash bilan.</summary>
    [AllowAnonymous]
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string sort = "latest", int page = 1)
    {
        var isAuth = User.Identity?.IsAuthenticated ?? false;
        var cleanSort = (sort ?? "latest").Trim().ToLowerInvariant();

        if (cleanSort == "mine" && !isAuth)
        {
            var returnUrl = $"/Quotes?sort=mine{(string.IsNullOrEmpty(q) ? "" : $"&q={Uri.EscapeDataString(q)}")}";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { requireLogin = true, loginUrl = $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}" });
            }
            return Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var quotes = await Mediator.Send(new GetAllQuotesQuery
        {
            Search = q,
            Sort = cleanSort,
            Page = page,
            PageSize = 12
        });

        var isAdmin = await SetIsAdminAsync();

        var vm = new QuotesPageViewModel
        {
            Quotes = quotes,
            Search = q,
            Sort = cleanSort,
            IsAdmin = isAdmin
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_QuotesFeedSection", vm);
        }

        return View("Index", vm);
    }

    /// <summary>Creates a quote (from the /Feed or /Quotes composer).</summary>
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuoteCommand command, string? returnUrl = null)
    {
        var quote = await Mediator.Send(command);
        _ = _telegramBot.BroadcastQuoteToGroupAsync(quote.Id, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return Redirect("/Feed");
    }

    /// <summary>
    /// Quote detail page with its comment thread, by internal id. Kept for internal/back-compat
    /// links (e.g. shared quotes in chat). Viewable without logging in.
    /// <summary>
    /// Sharable single quote view.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var detail = await Mediator.Send(new GetQuoteByIdQuery(id));
            await SetIsAdminAsync();
            return View("Details", detail);
        }
        catch (NotFoundException)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("QuoteDeleted");
        }
    }

    /// <summary>
    /// Canonical, shareable quote URL: /iqtibos/{username}/{slug}. Viewable without logging in.
    /// The username segment is for readability — the random slug alone identifies the quote.
    /// </summary>
    [HttpGet("/iqtibos/{username}/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> DetailsBySlug(string username, string slug)
    {
        try
        {
            var detail = await Mediator.Send(new GetQuoteBySlugQuery(slug));
            await SetIsAdminAsync();
            return View("Details", detail);
        }
        catch (NotFoundException)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("QuoteDeleted");
        }
    }

    [HttpPost("{id:int}/save")]
    public async Task<IActionResult> ToggleSave(int id)
    {
        var result = await Mediator.Send(new ToggleSaveQuoteCommand(id));
        return Json(result);
    }

    /// <summary>
    /// Records that the current user saw this quote in the feed (fired when the card scrolls
    /// into view) and returns the new total. Idempotent — counts once per user per quote.
    /// </summary>
    [HttpPost("{id:int}/view")]
    public async Task<IActionResult> RecordView(int id)
    {
        var viewCount = await Mediator.Send(new RecordQuoteViewCommand(id));
        return Json(new { viewCount });
    }

    [HttpPost("{id:int}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id)
    {
        var result = await Mediator.Send(new ToggleQuoteLikeCommand(id));
        return Json(result);
    }

    [HttpPost("{id:int}/comment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int id, [FromBody] AddQuoteCommentCommand command)
    {
        // The quote id always comes from the route; ignore any value in the body.
        var comment = await Mediator.Send(command with { QuoteId = id });
        return Json(comment);
    }

    [HttpPost("comment/{commentId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        await Mediator.Send(new DeleteQuoteCommentCommand(commentId));
        return NoContent();
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        await Mediator.Send(new DeleteQuoteCommand(id));

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return NoContent();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) &&
            Uri.TryCreate(referer, UriKind.Absolute, out var refUri) &&
            refUri.Host == Request.Host.Host &&
            !refUri.AbsolutePath.StartsWith("/quotes/" + id, StringComparison.OrdinalIgnoreCase) &&
            !refUri.AbsolutePath.StartsWith("/iqtibos/", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(refUri.PathAndQuery);
        }

        return Redirect("/Feed");
    }
}
