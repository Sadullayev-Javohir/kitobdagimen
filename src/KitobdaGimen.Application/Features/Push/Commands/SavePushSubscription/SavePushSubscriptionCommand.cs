using MediatR;

namespace KitobdaGimen.Application.Features.Push.Commands.SavePushSubscription;

/// <summary>Registers (or refreshes) the current user's Web Push subscription for a device.</summary>
public record SavePushSubscriptionCommand : IRequest<Unit>
{
    public string Endpoint { get; init; } = null!;
    public string P256dh { get; init; } = null!;
    public string Auth { get; init; } = null!;
}
