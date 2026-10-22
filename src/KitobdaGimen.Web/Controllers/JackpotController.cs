using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;
using KitobdaGimen.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

public class JackpotController : AppController
{
    private readonly IDailyJackpotService _jackpotService;

    public JackpotController(IDailyJackpotService jackpotService)
    {
        _jackpotService = jackpotService;
    }

    [HttpGet]
    [Route("/jackpot")]
    public async Task<IActionResult> Index()
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        var todayJackpot = await _jackpotService.GetTodayJackpotAsync(me?.Id);
        var history = await _jackpotService.GetHistoryAsync(me?.Id, limit: 30);

        ViewBag.CurrentUser = me;
        ViewBag.IsAdmin = me != null && me.Role >= UserRole.Admin;
        ViewBag.History = history;

        return View(todayJackpot);
    }

    [HttpGet]
    [Route("/jackpot/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        var jackpot = await _jackpotService.GetJackpotByIdAsync(id, me?.Id);
        if (jackpot is null)
        {
            return NotFound();
        }

        ViewBag.CurrentUser = me;
        ViewBag.IsAdmin = me != null && me.Role >= UserRole.Admin;

        return View(jackpot);
    }

    [HttpPost]
    [Authorize]
    [Route("/jackpot/{id:int}/like")]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        if (me is null) return Unauthorized();

        var (isLiked, count) = await _jackpotService.ToggleLikeAsync(id, me.Id);
        return Json(new { success = true, isLiked, count });
    }

    [HttpPost]
    [Authorize]
    [Route("/jackpot/{id:int}/comment")]
    public async Task<IActionResult> AddComment(int id, [FromBody] CommentRequest req)
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        if (me is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req?.Text))
        {
            return BadRequest(new { success = false, message = "Izoh matni kiritilmadi." });
        }

        var comment = await _jackpotService.AddCommentAsync(id, me.Id, req.Text);
        return Json(new { success = true, comment });
    }

    [HttpGet]
    [Route("/jackpot/{id:int}/likers")]
    public async Task<IActionResult> GetLikers(int id)
    {
        var likers = await _jackpotService.GetLikersAsync(id);
        return Json(likers);
    }

    [HttpPost]
    [Authorize]
    [Route("/jackpot/reroll")]
    public async Task<IActionResult> Reroll()
    {
        var me = await Mediator.Send(new GetCurrentUserQuery());
        if (me is null || me.Role < UserRole.Admin)
        {
            return Forbid();
        }

        var today = KitobdaGimen.Application.Common.UzTime.Today;
        var newJackpot = await _jackpotService.PickJackpotForDateAsync(today, broadcast: true);

        return RedirectToAction(nameof(Index));
    }

    public record CommentRequest(string Text);
}
