using KitobdaGimen.Application.Features.Push.Commands.DeletePushSubscription;
using KitobdaGimen.Application.Features.Push.Commands.SavePushSubscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Web Push endpoints: hand the browser the VAPID public key, then store/remove the
/// device subscription so the server can deliver real push notifications.
/// </summary>
[Authorize]
public class PushController : AppController
{
    private readonly IConfiguration _config;

    public PushController(IConfiguration config) => _config = config;

    /// <summary>VAPID public key the browser needs to subscribe (empty when push is not configured).</summary>
    [HttpGet("/push/public-key")]
    public IActionResult PublicKey() => Json(new { publicKey = _config["WebPush:PublicKey"] ?? "" });

    [HttpPost("/push/subscribe")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        if (request?.Endpoint is null || request.Keys is null)
        {
            return BadRequest();
        }

        await Mediator.Send(new SavePushSubscriptionCommand
        {
            Endpoint = request.Endpoint,
            P256dh = request.Keys.P256dh ?? "",
            Auth = request.Keys.Auth ?? ""
        });
        return Ok();
    }

    [HttpPost("/push/unsubscribe")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request?.Endpoint))
        {
            await Mediator.Send(new DeletePushSubscriptionCommand { Endpoint = request!.Endpoint });
        }
        return Ok();
    }

    public record PushSubscriptionRequest
    {
        public string? Endpoint { get; init; }
        public PushKeys? Keys { get; init; }
    }

    public record PushKeys
    {
        public string? P256dh { get; init; }
        public string? Auth { get; init; }
    }

    public record UnsubscribeRequest
    {
        public string? Endpoint { get; init; }
    }
}
