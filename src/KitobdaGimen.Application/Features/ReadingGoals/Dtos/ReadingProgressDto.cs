namespace KitobdaGimen.Application.Features.ReadingGoals.Dtos;

/// <summary>One day's logged reading toward a goal.</summary>
public record ReadingProgressDto
{
    public DateOnly Date { get; init; }
    public int PagesReadToday { get; init; }
}
