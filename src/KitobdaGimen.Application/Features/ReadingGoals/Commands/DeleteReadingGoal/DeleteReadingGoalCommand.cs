using MediatR;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.DeleteReadingGoal;

/// <summary>Removes a reading goal (book) and its progress from the current user's list.</summary>
public record DeleteReadingGoalCommand(int ReadingGoalId) : IRequest;
