using KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;
using KitobdaGimen.Application.Features.Quotes.Commands.AddQuoteComment;
using KitobdaGimen.Application.Features.Quotes.Commands.CreateQuote;
using KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuote;
using KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuoteComment;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleQuoteLike;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleSaveQuote;
using KitobdaGimen.Application.Features.Quotes.Queries.GetMyQuotes;
using KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;
using KitobdaGimen.Application.Features.Quotes.Queries.GetQuotes;
using KitobdaGimen.Application.Features.Quotes.Queries.GetSavedQuotes;
using KitobdaGimen.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("quotes")]
public class QuotesController : AppController
{
    /// <summary>Initial number of quotes rendered with the page; the rest stream in on scroll.</summary>
    private const int QuotePageSize = 4;

    /// <summary>Exposes whether the current user is an admin so quote cards can show moderation actions.</summary>
    private async Task SetIsAdminAsync()
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        ViewData["IsAdmin"] = me != null && me.Role >= UserRole.Admin;
    }

    /// <summary>All quotes (optionally filtered by book).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? bookId, string? q, int page = 1)
    {
        var quotes = await Mediator.Send(new GetQuotesQuery { BookId = bookId, Search = q, Page = page, PageSize = QuotePageSize });
        ViewData["Title"] = "Iqtiboslar";
        ViewData["Search"] = q;
        await SetIsAdminAsync();
        return View("Index", quotes);
    }

    [HttpGet("my")]
    public async Task<IActionResult> My(int page = 1)
    {
        var quotes = await Mediator.Send(new GetMyQuotesQuery { Page = page, PageSize = QuotePageSize });
        ViewData["Title"] = "Mening iqtiboslarim";
        await SetIsAdminAsync();
        return View("Index", quotes);
    }

    [HttpGet("saved")]
    public async Task<IActionResult> Saved(int page = 1)
    {
        var quotes = await Mediator.Send(new GetSavedQuotesQuery { Page = page, PageSize = QuotePageSize });
        ViewData["Title"] = "Saqlangan iqtiboslar";
        await SetIsAdminAsync();
        return View("Index", quotes);
    }

    /// <summary>Returns the next page of quote cards as an HTML fragment (infinite scroll).</summary>
    /// <param name="tab">Which list to page through: "my", "saved", or anything else for all quotes.</param>
    [HttpGet("cards")]
    public async Task<IActionResult> Cards(string? tab, int? bookId, string? q, int page = 2)
    {
        var quotes = tab switch
        {
            "my" => await Mediator.Send(new GetMyQuotesQuery { Page = page, PageSize = QuotePageSize }),
            "saved" => await Mediator.Send(new GetSavedQuotesQuery { Page = page, PageSize = QuotePageSize }),
            _ => await Mediator.Send(new GetQuotesQuery { BookId = bookId, Search = q, Page = page, PageSize = QuotePageSize }),
        };
        await SetIsAdminAsync();
        return PartialView("_QuoteCards", quotes.Items);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuoteCommand command)
    {
        await Mediator.Send(command);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Quote detail page with its comment thread, by internal id. Viewable without
    /// logging in so shared quotes (e.g. sent to someone in chat) open for anonymous visitors too.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await Mediator.Send(new GetQuoteByIdQuery(id));
        return View("Details", detail);
    }

    [HttpPost("{id:int}/save")]
    public async Task<IActionResult> ToggleSave(int id)
    {
        var result = await Mediator.Send(new ToggleSaveQuoteCommand(id));
        return Json(result);
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
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteQuoteCommand(id));
        return RedirectToAction(nameof(My));
    }
}
