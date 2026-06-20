using System.Security.Claims;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Web.Hubs;

/// <summary>
/// Real-time chat hub. Each connection joins a per-user group ("user-{id}") so messages
/// can be pushed to every device a user has open. Authenticated via the JWT cookie.
/// Also tracks online presence (Redis) and broadcasts online/offline changes to the
/// user's accepted connections.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IPresenceService _presence;
    private readonly IChatNotifier _chatNotifier;
    private readonly IAppDbContext _db;

    public ChatHub(IPresenceService presence, IChatNotifier chatNotifier, IAppDbContext db)
    {
        _presence = presence;
        _chatNotifier = chatNotifier;
        _db = db;
    }

    /// <summary>The SignalR group name that targets a single user across all their connections.</summary>
    public static string UserGroup(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            var becameOnline = await _presence.SetOnlineAsync(userId);
            if (becameOnline)
            {
                await BroadcastPresenceAsync(userId, isOnline: true, lastSeenAt: null);
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetUserId(out var userId))
        {
            var becameOffline = await _presence.SetOfflineAsync(userId);
            if (becameOffline)
            {
                await BroadcastPresenceAsync(userId, isOnline: false, lastSeenAt: DateTime.UtcNow);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Called periodically by clients to keep the presence TTL alive.</summary>
    public async Task Heartbeat()
    {
        if (TryGetUserId(out var userId))
        {
            await _presence.SetOnlineAsync(userId);
        }
    }

    private async Task BroadcastPresenceAsync(int userId, bool isOnline, DateTime? lastSeenAt)
    {
        var partnerIds = await _db.Connections
            .Where(c => c.Status == ConnectionStatus.Accepted
                        && (c.RequesterId == userId || c.AddresseeId == userId))
            .Select(c => c.RequesterId == userId ? c.AddresseeId : c.RequesterId)
            .ToListAsync();

        await _chatNotifier.PresenceChangedAsync(partnerIds, userId, isOnline, lastSeenAt);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
