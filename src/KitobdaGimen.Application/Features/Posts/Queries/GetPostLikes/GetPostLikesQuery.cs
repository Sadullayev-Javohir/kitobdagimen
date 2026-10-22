using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetPostLikes;

public record GetPostLikesQuery : IRequest<PagedResult<PostLikerDto>>
{
    public int PostId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
