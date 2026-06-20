using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.ReadingGoals.Dtos;

/// <summary>A reading goal with derived progress figures.</summary>
public record ReadingGoalDto
{
    public int Id { get; init; }
    public BookSummaryDto Book { get; init; } = null!;
    public int TotalPages { get; init; }
    public int DailyPageGoal { get; init; }
    public DateTime StartDate { get; init; }
    public int CurrentPage { get; init; }
    public bool IsActive { get; init; }

    /// <summary>Pages logged today toward this goal.</summary>
    public int PagesReadToday { get; init; }

    public int ProgressPercent =>
        TotalPages > 0 ? (int)Math.Round(Math.Min(CurrentPage, TotalPages) * 100.0 / TotalPages) : 0;

    public int PagesRemaining => Math.Max(0, TotalPages - CurrentPage);
}
