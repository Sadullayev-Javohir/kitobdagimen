using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A quote a user saved from a book. Other users can save it to their own collection.
/// </summary>
public class Quote : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<SavedQuote> SavedBy { get; set; } = new List<SavedQuote>();
    public ICollection<QuoteLike> Likes { get; set; } = new List<QuoteLike>();
    public ICollection<QuoteComment> Comments { get; set; } = new List<QuoteComment>();
}
