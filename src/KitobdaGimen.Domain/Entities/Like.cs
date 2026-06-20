using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A "like" placed by a user on a post. (PostId, UserId) is unique.
/// </summary>
public class Like : BaseEntity
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
