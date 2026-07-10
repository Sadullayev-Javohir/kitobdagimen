using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.DeleteProgressEntry;

/// <summary>
/// Undoes a mistaken daily reading log: removes the whole entry for the given date,
/// recomputes the current page from the remaining logs, and reactivates the book if it
/// was mistakenly marked finished. Other days' history is kept intact.
/// </summary>
public record DeleteProgressEntryCommand : IRequest<ReadingGoalDto>
{
    public int ReadingGoalId { get; init; }

    /// <summary>The day whose logged pages should be removed.</summary>
    public DateOnly Date { get; init; }
}
