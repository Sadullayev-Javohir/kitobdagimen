using KitobdaGimen.Application.Features.Connections.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Connections.Commands.SendConnectionRequest;

/// <summary>
/// Sends a chat invite to another user. If the other user has already invited the current
/// user, the existing invite is auto-accepted instead.
/// </summary>
public record SendConnectionRequestCommand(int AddresseeId) : IRequest<ConnectionDto>;
