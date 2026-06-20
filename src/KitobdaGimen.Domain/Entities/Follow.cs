using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Directed follow relationship: <see cref="FollowerId"/> follows <see cref="FollowingId"/>.
/// (FollowerId, FollowingId) is unique.
/// </summary>
public class Follow : BaseEntity
{
    public int FollowerId { get; set; }
    public User Follower { get; set; } = null!;

    public int FollowingId { get; set; }
    public User Following { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
