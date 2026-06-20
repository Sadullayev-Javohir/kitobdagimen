using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Daily log of how many pages a user read toward a <see cref="ReadingGoal"/> on a given date.
/// </summary>
public class ReadingProgress : BaseEntity
{
    public int ReadingGoalId { get; set; }
    public ReadingGoal ReadingGoal { get; set; } = null!;

    public DateOnly Date { get; set; }
    public int PagesReadToday { get; set; }
}
