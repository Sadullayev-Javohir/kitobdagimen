using KitobdaGimen.Application.Features.Auth.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>Returns the currently authenticated user, or null if anonymous.</summary>
public record GetCurrentUserQuery : IRequest<UserDto?>;
