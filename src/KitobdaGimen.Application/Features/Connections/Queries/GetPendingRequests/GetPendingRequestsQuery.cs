using KitobdaGimen.Application.Features.Connections.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Connections.Queries.GetPendingRequests;

/// <summary>Incoming pending invites the current user has received (to accept/decline).</summary>
public record GetPendingRequestsQuery : IRequest<IReadOnlyList<ConnectionDto>>;
