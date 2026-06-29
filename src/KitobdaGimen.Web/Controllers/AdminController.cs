using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeletePost;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteQuote;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteUser;
using KitobdaGimen.Application.Features.Admin.Commands.SetUserRole;
using KitobdaGimen.Application.Features.Admin.Queries.GetAdminUsers;
using KitobdaGimen.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Admin panel. Authorization is enforced in the Application handlers (role read from DB).
/// Admin: delete any post/quote. SuperAdmin: also promote/demote admins and delete users.
/// </summary>
[Authorize]
[Route("admin")]
public class AdminController : AppController
{
    /// <summary>User list with exact registration and last-seen timestamps.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var users = await Mediator.Send(new GetAdminUsersQuery());
            var myRole = users.FirstOrDefault(u => u.Id == CurrentUserId)?.Role ?? UserRole.User;
            ViewData["MyRole"] = myRole;
            return View(users);
        }
        catch (Exception ex) when (ex is ForbiddenAccessException or UnauthorizedAccessException)
        {
            // Hide the panel from non-admins.
            return RedirectToAction("Index", "Feed");
        }
    }

    [HttpPost("users/{id:int}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, [FromForm] bool makeAdmin)
    {
        await Mediator.Send(new SetUserRoleCommand(id, makeAdmin));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("users/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await Mediator.Send(new AdminDeleteUserCommand(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("posts/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        await Mediator.Send(new AdminDeletePostCommand(id));
        return Ok();
    }

    [HttpPost("quotes/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuote(int id)
    {
        await Mediator.Send(new AdminDeleteQuoteCommand(id));
        return Ok();
    }
}
