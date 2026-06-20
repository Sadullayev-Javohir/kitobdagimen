using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Follow.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Follow.Queries.GetFollowers;

/// <summary>Returns the users who follow the given user, paged.</summary>
public record GetFollowersQuery : IRequest<PagedResult<FollowUserDto>>
{
    public int UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
