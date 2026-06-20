using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.ReadingGoals.Queries.GetActiveReadingGoals;

/// <summary>
/// Returns a user's active reading goals. When <see cref="UserId"/> is null the
/// current user is used.
/// </summary>
public record GetActiveReadingGoalsQuery(int? UserId = null) : IRequest<IReadOnlyList<ReadingGoalDto>>;
