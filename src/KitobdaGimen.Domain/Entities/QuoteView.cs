using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Records that a user viewed a quote (used for view counts / "ko'rishlar").
/// A view is registered when the quote card scrolls into the feed — one per user per quote.
/// </summary>
public class QuoteView : BaseEntity
{
    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ViewedAt { get; set; }
}
