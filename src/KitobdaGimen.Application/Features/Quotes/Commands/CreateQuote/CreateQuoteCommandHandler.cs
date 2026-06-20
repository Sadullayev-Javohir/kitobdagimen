using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Commands.CreateQuote;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, QuoteDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public CreateQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<QuoteDto> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var bookExists = await _db.Books.AnyAsync(b => b.Id == request.BookId, cancellationToken);
        if (!bookExists)
        {
            throw new NotFoundException("Kitob", request.BookId);
        }

        var quote = new Quote
        {
            UserId = userId,
            BookId = request.BookId,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        };

        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.Quotes
            .Where(q => q.Id == quote.Id)
            .ToQuoteDto(userId)
            .FirstAsync(cancellationToken);

        // Notify the author's followers that a new quote was shared.
        var followerIds = await _db.Follows
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);
        if (followerIds.Count > 0)
        {
            await _notifications.NotifyManyAsync(followerIds, new NotificationDto
            {
                Type = "quote",
                ActorId = dto.Author.Id,
                ActorName = dto.Author.FullName,
                ActorAvatarUrl = dto.Author.AvatarUrl,
                Message = $"{dto.Author.FullName} yangi iqtibos qo'shdi",
                Url = "/quotes"
            }, cancellationToken);
        }

        return dto;
    }
}
