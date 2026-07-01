using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Commands.ToggleChallengeWinnerLike;

public class ToggleChallengeWinnerLikeCommandHandler
    : IRequestHandler<ToggleChallengeWinnerLikeCommand, ChallengeLikeResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleChallengeWinnerLikeCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ChallengeLikeResultDto> Handle(
        ToggleChallengeWinnerLikeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var exists = await _db.ChallengeWinners
            .AnyAsync(w => w.Id == request.ChallengeWinnerId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Challenge g'olibi", request.ChallengeWinnerId);
        }

        var existing = await _db.ChallengeWinnerLikes
            .FirstOrDefaultAsync(
                l => l.ChallengeWinnerId == request.ChallengeWinnerId && l.UserId == userId,
                cancellationToken);

        bool liked;
        if (existing is null)
        {
            _db.ChallengeWinnerLikes.Add(new ChallengeWinnerLike
            {
                ChallengeWinnerId = request.ChallengeWinnerId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            liked = true;
        }
        else
        {
            _db.ChallengeWinnerLikes.Remove(existing);
            liked = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var likeCount = await _db.ChallengeWinnerLikes
            .CountAsync(l => l.ChallengeWinnerId == request.ChallengeWinnerId, cancellationToken);

        return new ChallengeLikeResultDto { Liked = liked, LikeCount = likeCount };
    }
}
