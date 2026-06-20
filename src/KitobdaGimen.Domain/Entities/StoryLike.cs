using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>A "like" placed by a user on a story. (StoryId, UserId) is unique.</summary>
public class StoryLike : BaseEntity
{
    public int StoryId { get; set; }
    public Story Story { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
