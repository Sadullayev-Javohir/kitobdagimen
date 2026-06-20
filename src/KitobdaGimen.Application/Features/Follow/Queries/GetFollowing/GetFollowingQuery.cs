using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Follow.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Follow.Queries.GetFollowing;

/// <summary>Returns the users the given user follows, paged.</summary>
public record GetFollowingQuery : IRequest<PagedResult<FollowUserDto>>
{
    public int UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
