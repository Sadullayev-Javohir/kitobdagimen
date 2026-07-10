namespace KitobdaGimen.Application.Features.ReadingGoals.Dtos;

/// <summary>A reading goal together with its daily progress history.</summary>
public record ReadingGoalDetailDto
{
    public ReadingGoalDto Goal { get; init; } = null!;
    public IReadOnlyList<ReadingProgressDto> History { get; init; } = Array.Empty<ReadingProgressDto>();

    /// <summary>True when the current user owns this goal and may edit its progress.</summary>
    public bool IsOwner { get; init; }
}
