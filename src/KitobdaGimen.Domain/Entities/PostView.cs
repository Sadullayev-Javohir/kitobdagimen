using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Records that a user viewed a post (used for view counts / "ko'rishlar").
/// </summary>
public class PostView : BaseEntity
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ViewedAt { get; set; }
}
