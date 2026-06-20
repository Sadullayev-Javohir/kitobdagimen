using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Features.ReadingGoals;

internal static class ReadingGoalQueryableExtensions
{
    /// <summary>Projects reading goals to <see cref="ReadingGoalDto"/>, including today's logged pages.</summary>
    public static IQueryable<ReadingGoalDto> ToReadingGoalDto(this IQueryable<ReadingGoal> query, DateOnly today)
    {
        return query.Select(g => new ReadingGoalDto
        {
            Id = g.Id,
            Book = new BookSummaryDto
            {
                Id = g.Book.Id,
                Title = g.Book.Title,
                Author = g.Book.Author,
                CoverUrl = g.Book.CoverUrl
            },
            TotalPages = g.Book.TotalPages,
            DailyPageGoal = g.DailyPageGoal,
            StartDate = g.StartDate,
            CurrentPage = g.CurrentPage,
            IsActive = g.IsActive,
            PagesReadToday = g.Progress
                .Where(p => p.Date == today)
                .Sum(p => (int?)p.PagesReadToday) ?? 0
        });
    }
}
