using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Commands.ToggleLike;

/// <summary>Likes the post if not already liked by the current user, otherwise removes the like.</summary>
public record ToggleLikeCommand(int PostId) : IRequest<LikeResultDto>;
