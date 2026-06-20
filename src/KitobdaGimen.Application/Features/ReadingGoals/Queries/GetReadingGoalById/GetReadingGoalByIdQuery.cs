using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.ReadingGoals.Queries.GetReadingGoalById;

/// <summary>Returns a reading goal with its progress history (owner only).</summary>
public record GetReadingGoalByIdQuery(int ReadingGoalId) : IRequest<ReadingGoalDetailDto>;
