using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KitobdaGimen.Web.Hubs;

/// <summary>
/// Real-time activity notifications (likes, comments, follows). Connections join a
/// per-user group so notifications reach all of the recipient's open clients.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public static string UserGroup(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(value, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }
        await base.OnConnectedAsync();
    }
}
