using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A "like" placed by a user on a <see cref="Quote"/>. (QuoteId, UserId) is unique.
/// </summary>
public class QuoteLike : BaseEntity
{
    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
