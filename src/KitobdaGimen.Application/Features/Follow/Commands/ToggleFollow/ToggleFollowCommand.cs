using KitobdaGimen.Application.Features.Follow.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Follow.Commands.ToggleFollow;

/// <summary>Follows the target user if not already followed, otherwise unfollows.</summary>
public record ToggleFollowCommand(int TargetUserId) : IRequest<FollowResultDto>;
