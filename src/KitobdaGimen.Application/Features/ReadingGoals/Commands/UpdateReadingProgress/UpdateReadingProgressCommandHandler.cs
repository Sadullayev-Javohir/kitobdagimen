using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.UpdateReadingProgress;

public class UpdateReadingProgressCommandHandler : IRequestHandler<UpdateReadingProgressCommand, ReadingGoalDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public UpdateReadingProgressCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<ReadingGoalDto> Handle(UpdateReadingProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var goal = await _db.ReadingGoals
            .Include(g => g.Book)
            .FirstOrDefaultAsync(g => g.Id == request.ReadingGoalId, cancellationToken)
            ?? throw new NotFoundException("O'qish maqsadi", request.ReadingGoalId);

        if (goal.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var progress = await _db.ReadingProgress
            .FirstOrDefaultAsync(p => p.ReadingGoalId == goal.Id && p.Date == today, cancellationToken);

        int pagesToday;
        if (progress is null)
        {
            _db.ReadingProgress.Add(new ReadingProgress
            {
                ReadingGoalId = goal.Id,
                Date = today,
                PagesReadToday = request.PagesRead
            });
            pagesToday = request.PagesRead;
        }
        else
        {
            progress.PagesReadToday += request.PagesRead;
            pagesToday = progress.PagesReadToday;
        }

        var totalPages = goal.Book.TotalPages;
        goal.CurrentPage += request.PagesRead;
        var bookFinished = false;
        if (totalPages > 0 && goal.CurrentPage >= totalPages)
        {
            goal.CurrentPage = totalPages;
            goal.IsActive = false; // book finished
            bookFinished = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Kuzatuvchilarga "bugun shu kitobdan N bet o'qidi" xabarini yuboramiz
        // (faqat haqiqatan bet kiritilganda — bo'sh/0 log'da xabar bermaymiz).
        if (request.PagesRead > 0)
        {
            await NotifyFollowersAsync(userId, goal.Book.Title, pagesToday, bookFinished, cancellationToken);
        }

        return await _db.ReadingGoals
            .Where(g => g.Id == goal.Id)
            .ToReadingGoalDto(today)
            .FirstAsync(cancellationToken);
    }

    private async Task NotifyFollowersAsync(
        int userId, string bookTitle, int pagesToday, bool bookFinished, CancellationToken cancellationToken)
    {
        var followerIds = await _db.Follows
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);
        if (followerIds.Count == 0)
        {
            return;
        }

        var actor = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FullName, u.AvatarUrl, u.Username })
            .FirstAsync(cancellationToken);

        var message = bookFinished
            ? $"{actor.FullName} «{bookTitle}» kitobini tugatdi 🎉"
            : $"{actor.FullName} bugun «{bookTitle}» kitobidan {pagesToday} bet o'qidi";

        await _notifications.NotifyManyAsync(followerIds, new NotificationDto
        {
            Type = "reading",
            ActorId = userId,
            ActorName = actor.FullName,
            ActorAvatarUrl = actor.AvatarUrl,
            Message = message,
            Url = $"/profile/{(string.IsNullOrWhiteSpace(actor.Username) ? userId.ToString() : actor.Username)}"
        }, cancellationToken);
    }
}
