using MediatR;

namespace KitobdaGimen.Application.Features.Connections.Commands.CancelConnectionRequest;

/// <summary>Cancels a pending invite the current user sent (only the requester may cancel).</summary>
public record CancelConnectionRequestCommand(int ConnectionId) : IRequest<Unit>;
