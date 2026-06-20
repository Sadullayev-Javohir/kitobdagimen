using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Onboarding.Commands.SaveUserGenres;

public class SaveUserGenresCommandHandler : IRequestHandler<SaveUserGenresCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SaveUserGenresCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SaveUserGenresCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        // Only keep genre ids that actually exist.
        var validGenreIds = await _db.Genres
            .Where(g => request.GenreIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var existing = await _db.UserGenres
            .Where(ug => ug.UserId == userId)
            .ToListAsync(cancellationToken);

        _db.UserGenres.RemoveRange(existing);

        foreach (var genreId in validGenreIds)
        {
            _db.UserGenres.Add(new UserGenre { UserId = userId, GenreId = genreId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
