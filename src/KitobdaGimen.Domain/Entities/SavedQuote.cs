using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Links a user to a <see cref="Quote"/> they bookmarked. (QuoteId, UserId) is unique.
/// </summary>
public class SavedQuote : BaseEntity
{
    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
