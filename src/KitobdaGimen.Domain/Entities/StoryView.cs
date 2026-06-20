using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>Records that a user viewed a story. (StoryId, UserId) is unique.</summary>
public class StoryView : BaseEntity
{
    public int StoryId { get; set; }
    public Story Story { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ViewedAt { get; set; }
}
