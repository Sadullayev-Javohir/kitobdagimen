using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Commands.ToggleChallengeWinnerLike;

/// <summary>Challenge g'olibiga like qo'yish/olib tashlash (toggle).</summary>
public record ToggleChallengeWinnerLikeCommand(int ChallengeWinnerId)
    : IRequest<ChallengeLikeResultDto>;

/// <summary>Like toggle natijasi.</summary>
public record ChallengeLikeResultDto
{
    public bool Liked { get; init; }
    public int LikeCount { get; init; }
}
