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
    private readonly Monitoring.RealtimeConnectionCounter _connections;

    public NotificationHub(Monitoring.RealtimeConnectionCounter connections)
    {
        _connections = connections;
    }

    public static string UserGroup(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        _connections.Increment();
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(value, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.Decrement();
        await base.OnDisconnectedAsync(exception);
    }
}
