using KitobdaGimen.Application.Features.Notifications.Commands.MarkNotificationsRead;
using KitobdaGimen.Application.Features.Notifications.Queries.GetUnreadNotifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("notifications")]
public class NotificationsController : AppController
{
    /// <summary>Unread notifications for the current user (newest first). The client uses the count
    /// for the navbar badge and replays missed invites — so an invite sent while the user was
    /// offline surfaces the moment they next load any page.</summary>
    [HttpGet("unread")]
    public async Task<IActionResult> Unread()
    {
        var items = await Mediator.Send(new GetUnreadNotificationsQuery());
        return Json(new { count = items.Count, items });
    }

    /// <summary>Marks notifications read (all unread, or just the posted ids).</summary>
    [HttpPost("read")]
    public async Task<IActionResult> Read([FromBody] MarkReadRequest? body)
    {
        await Mediator.Send(new MarkNotificationsReadCommand(body?.Ids));
        return NoContent();
    }

    /// <summary>JSON body for <see cref="Read"/>; null/empty ids means "mark all read".</summary>
    public record MarkReadRequest(IReadOnlyList<int>? Ids);
}
