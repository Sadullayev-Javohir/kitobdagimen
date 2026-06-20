using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.UpdateReadingProgress;

/// <summary>Logs pages read today against a goal, advancing the current page.</summary>
public record UpdateReadingProgressCommand : IRequest<ReadingGoalDto>
{
    public int ReadingGoalId { get; init; }

    /// <summary>Number of pages read in this update (added to today's total and the current page).</summary>
    public int PagesRead { get; init; }
}
