using KitobdaGimen.Application.Features.ReadingGoals.Dtos;

namespace KitobdaGimen.Web.Models;

/// <summary>View model for the library page: active goals plus finished books.</summary>
public class ReadingBooksPageViewModel
{
    public IReadOnlyList<ReadingGoalDto> Active { get; init; } = Array.Empty<ReadingGoalDto>();
    public IReadOnlyList<ReadingGoalDto> Finished { get; init; } = Array.Empty<ReadingGoalDto>();
}
