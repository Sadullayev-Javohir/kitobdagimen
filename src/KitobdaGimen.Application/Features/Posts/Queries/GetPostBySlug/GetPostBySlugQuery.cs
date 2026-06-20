using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetPostBySlug;

/// <summary>Returns a single post (by its public slug) with its comment thread, or throws if it does not exist.</summary>
public record GetPostBySlugQuery(string Slug) : IRequest<PostDetailDto>;
