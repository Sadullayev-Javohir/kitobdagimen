using KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;
using KitobdaGimen.Application.Features.Quotes.Commands.AddQuoteComment;
using KitobdaGimen.Application.Features.Quotes.Commands.CreateQuote;
using KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuote;
using KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuoteComment;
using KitobdaGimen.Application.Features.Quotes.Commands.RecordQuoteView;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleQuoteLike;
using KitobdaGimen.Application.Features.Quotes.Commands.ToggleSaveQuote;
using KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;
using KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteBySlug;
using KitobdaGimen.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("quotes")]
public class QuotesController : AppController
{
    /// <summary>Exposes whether the current user is an admin so quote cards can show moderation actions.</summary>
    private async Task SetIsAdminAsync()
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        ViewData["IsAdmin"] = me != null && me.Role >= UserRole.Admin;
    }

    /// <summary>Creates a quote (from the /Feed composer) and returns to the feed.</summary>
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuoteCommand command)
    {
        await Mediator.Send(command);
        return Redirect("/Feed");
    }

    /// <summary>
    /// Quote detail page with its comment thread, by internal id. Kept for internal/back-compat
    /// links (e.g. shared quotes in chat). Viewable without logging in.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await Mediator.Send(new GetQuoteByIdQuery(id));
        await SetIsAdminAsync();
        return View("Details", detail);
    }

    /// <summary>
    /// Canonical, shareable quote URL: /iqtibos/{username}/{slug}. Viewable without logging in.
    /// The username segment is for readability — the random slug alone identifies the quote.
    /// </summary>
    [HttpGet("/iqtibos/{username}/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> DetailsBySlug(string username, string slug)
    {
        var detail = await Mediator.Send(new GetQuoteBySlugQuery(slug));
        await SetIsAdminAsync();
        return View("Details", detail);
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
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteQuoteCommand(id));
        return Redirect("/profile");
    }
}
