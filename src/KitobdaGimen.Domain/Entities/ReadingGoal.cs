using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A user's reading goal for a specific book: how many pages per day, and current progress.
/// </summary>
public class ReadingGoal : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int DailyPageGoal { get; set; }
    public DateTime StartDate { get; set; }
    public int CurrentPage { get; set; }
    public bool IsActive { get; set; }

    // Navigation
    public ICollection<ReadingProgress> Progress { get; set; } = new List<ReadingProgress>();
}
