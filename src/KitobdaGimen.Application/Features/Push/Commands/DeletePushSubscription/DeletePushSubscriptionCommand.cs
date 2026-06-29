using MediatR;

namespace KitobdaGimen.Application.Features.Push.Commands.DeletePushSubscription;

/// <summary>Removes a Web Push subscription by its endpoint (on unsubscribe / logout).</summary>
public record DeletePushSubscriptionCommand : IRequest<Unit>
{
    public string Endpoint { get; init; } = null!;
}
