using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.ReadingGoals.Queries.GetFinishedReadingGoals;

/// <summary>
/// Returns a user's finished reading goals (book completed). When <see cref="UserId"/>
/// is null the current user is used.
/// </summary>
public record GetFinishedReadingGoalsQuery(int? UserId = null) : IRequest<IReadOnlyList<ReadingGoalDto>>;
