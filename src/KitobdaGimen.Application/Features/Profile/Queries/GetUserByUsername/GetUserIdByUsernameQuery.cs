using MediatR;

namespace KitobdaGimen.Application.Features.Profile.Queries.GetUserByUsername;

/// <summary>
/// Resolves a unique username to its user id (case-insensitive), or throws if not found.
/// Used by the shareable public profile route <c>/u/{username}</c>.
/// </summary>
public record GetUserIdByUsernameQuery(string Username) : IRequest<int>;
