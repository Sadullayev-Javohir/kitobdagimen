using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Profile.Queries.GetUserPosts;

/// <summary>Returns a user's own posts, newest first, paged.</summary>
public record GetUserPostsQuery : IRequest<PagedResult<PostDto>>
{
    public int UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
