using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.CreateReadingGoal;

/// <summary>Starts a new active reading goal for a book for the current user.</summary>
public record CreateReadingGoalCommand : IRequest<ReadingGoalDto>
{
    public int BookId { get; init; }
    public int DailyPageGoal { get; init; }
    public DateTime? StartDate { get; init; }
}
