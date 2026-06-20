using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetPostById;

/// <summary>Returns a single post with its comment thread, or throws if it does not exist.</summary>
public record GetPostByIdQuery(int PostId) : IRequest<PostDetailDto>;
