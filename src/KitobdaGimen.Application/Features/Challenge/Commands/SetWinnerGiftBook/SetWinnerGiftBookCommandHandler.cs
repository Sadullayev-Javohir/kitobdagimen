using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Admin;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Commands.SetWinnerGiftBook;

public class SetWinnerGiftBookCommandHandler : IRequestHandler<SetWinnerGiftBookCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetWinnerGiftBookCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SetWinnerGiftBookCommand request, CancellationToken cancellationToken)
    {
        // Sovg'ani faqat super admin bera oladi.
        var (callerId, _) = await AdminGuard.RequireAsync(
            _db, _currentUser, UserRole.SuperAdmin, cancellationToken);

        var winner = await _db.ChallengeWinners
            .FirstOrDefaultAsync(
                w => w.Year == request.Year && w.Month == request.Month && w.Rank == request.Rank,
                cancellationToken)
            ?? throw new NotFoundException(
                "Challenge g'olibi", $"{request.Year}-{request.Month} #{request.Rank}");

        var title = request.GiftBookTitle?.Trim();
        var author = request.GiftBookAuthor?.Trim();
        var cover = request.GiftBookCoverUrl?.Trim();

        winner.GiftBookTitle = string.IsNullOrEmpty(title) ? null : title;
        winner.GiftBookAuthor = string.IsNullOrEmpty(author) ? null : author;

        // Yangi muqova yuklangan bo'lsagina almashtiramiz — aks holda mavjud muqova saqlanadi
        // (faqat nom/muallifni tahrirlaganda rasm o'chib ketmasin).
        if (!string.IsNullOrEmpty(cover))
        {
            winner.GiftBookCoverUrl = cover;
        }

        var hasGift = !string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(winner.GiftBookCoverUrl);
        winner.GiftedByUserId = hasGift ? callerId : null;
        winner.GiftedAt = hasGift ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
