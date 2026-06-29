using System.Net;
using System.Text.Json;
using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;
using WebPushSubscription = WebPush.PushSubscription;

namespace KitobdaGimen.Infrastructure.Push;

/// <summary>
/// Sends Web Push notifications via VAPID to all of a user's registered devices. Expired
/// subscriptions (404/410 from the push service) are pruned. Best-effort: any failure is logged,
/// never thrown, so notification delivery never breaks the originating request.
/// </summary>
public class WebPushSender : IPushSender
{
    private readonly IAppDbContext _db;
    private readonly ILogger<WebPushSender> _logger;
    private readonly string? _publicKey;
    private readonly string? _privateKey;
    private readonly string _subject;
    private readonly WebPushClient _client = new();

    public WebPushSender(IAppDbContext db, IConfiguration config, ILogger<WebPushSender> logger)
    {
        _db = db;
        _logger = logger;
        _publicKey = config["WebPush:PublicKey"];
        _privateKey = config["WebPush:PrivateKey"];
        _subject = config["WebPush:Subject"] is { Length: > 0 } s ? s : "mailto:admin@kitobdagimen.uz";
    }

    public async Task SendAsync(int recipientUserId, PushNotificationPayload payload, CancellationToken cancellationToken = default)
    {
        // VAPID kalitlari sozlanmagan bo'lsa — jim o'tkazamiz (sayt ishlayveradi).
        if (string.IsNullOrWhiteSpace(_publicKey) || string.IsNullOrWhiteSpace(_privateKey))
        {
            return;
        }

        var subs = await _db.PushSubscriptions
            .Where(s => s.UserId == recipientUserId)
            .ToListAsync(cancellationToken);
        if (subs.Count == 0)
        {
            return;
        }

        var vapid = new VapidDetails(_subject, _publicKey, _privateKey);
        var json = JsonSerializer.Serialize(new
        {
            title = payload.Title,
            body = payload.Body,
            url = payload.Url,
            icon = payload.Icon,
            tag = payload.Tag
        });

        var gone = new List<Domain.Entities.PushSubscription>();
        foreach (var s in subs)
        {
            try
            {
                await _client.SendNotificationAsync(
                    new WebPushSubscription(s.Endpoint, s.P256dh, s.Auth), json, vapid);
            }
            catch (WebPushException ex) when (
                ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Obuna bekor qilingan / muddati o'tgan — tozalaymiz.
                gone.Add(s);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Web push yuborib bo'lmadi (user {UserId}).", recipientUserId);
            }
        }

        if (gone.Count > 0)
        {
            _db.PushSubscriptions.RemoveRange(gone);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
