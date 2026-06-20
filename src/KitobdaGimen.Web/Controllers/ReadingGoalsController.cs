using KitobdaGimen.Application.Features.ReadingGoals.Commands.CreateReadingGoal;
using KitobdaGimen.Application.Features.ReadingGoals.Commands.DeleteReadingGoal;
using KitobdaGimen.Application.Features.ReadingGoals.Commands.UpdateReadingProgress;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetActiveReadingGoals;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetFinishedReadingGoals;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetReadingGoalById;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("reading-books")]
[Route("reading-goals")]
public class ReadingGoalsController : AppController
{
    /// <summary>The current user's active reading goals.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var active = await Mediator.Send(new GetActiveReadingGoalsQuery());
        var finished = await Mediator.Send(new GetFinishedReadingGoalsQuery());
        return View(new ReadingBooksPageViewModel { Active = active, Finished = finished });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await Mediator.Send(new GetReadingGoalByIdQuery(id));
        return View(detail);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReadingGoalCommand command)
    {
        await Mediator.Send(command);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("progress")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateReadingProgressCommand command)
    {
        var goal = await Mediator.Send(command);
        return Json(goal);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteReadingGoalCommand(id));
        return Ok();
    }
}
