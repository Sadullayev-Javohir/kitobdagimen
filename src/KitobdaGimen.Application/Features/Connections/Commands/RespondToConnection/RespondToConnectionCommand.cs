using KitobdaGimen.Application.Features.Connections.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Connections.Commands.RespondToConnection;

/// <summary>Accepts or declines a pending chat invite. Only the addressee may respond.</summary>
public record RespondToConnectionCommand(int ConnectionId, bool Accept) : IRequest<ConnectionDto>;
