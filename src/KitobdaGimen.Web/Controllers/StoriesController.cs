using KitobdaGimen.Application.Features.Stories.Commands.CreateStory;
using KitobdaGimen.Application.Features.Stories.Commands.DeleteStory;
using KitobdaGimen.Application.Features.Stories.Commands.RecordStoryView;
using KitobdaGimen.Application.Features.Stories.Commands.ToggleStoryLike;
using KitobdaGimen.Application.Features.Stories.Queries.GetStoryById;
using KitobdaGimen.Application.Features.Stories.Queries.GetUserStories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("stories")]
public class StoriesController : AppController
{
    /// <summary>Returns a user's stories for the viewer (JSON).</summary>
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> ForUser(int userId)
    {
        var stories = await Mediator.Send(new GetUserStoriesQuery(userId));
        return Json(stories);
    }

    /// <summary>
    /// A single story's detail page: image, title, text, date, like and view counts.
    /// Anonymous-friendly so a shared link works for not-yet-registered users.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var story = await Mediator.Send(new GetStoryByIdQuery(id));
        ViewData["Title"] = string.IsNullOrWhiteSpace(story.Title) ? "Story" : story.Title;
        return View(story);
    }

    /// <summary>Creates a story and returns it (JSON).</summary>
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateStoryCommand command)
    {
        var story = await Mediator.Send(command);
        return Json(story);
    }

    /// <summary>Records a view for the current user and returns the new view count (JSON).</summary>
    [HttpPost("{id:int}/view")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> View(int id)
    {
        var viewCount = await Mediator.Send(new RecordStoryViewCommand(id));
        return Json(new { viewCount });
    }

    /// <summary>Likes or un-likes a story (JSON).</summary>
    [HttpPost("{id:int}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id)
    {
        var result = await Mediator.Send(new ToggleStoryLikeCommand(id));
        return Json(result);
    }

    /// <summary>Deletes a story (only the author may do this).</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteStoryCommand(id));
        return NoContent();
    }
}
