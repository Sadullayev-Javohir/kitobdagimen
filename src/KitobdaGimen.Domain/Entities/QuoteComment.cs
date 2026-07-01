using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A comment on a <see cref="Quote"/>. Supports one level of threaded replies via
/// <see cref="ParentCommentId"/> — mirrors <see cref="Comment"/> on posts.
/// </summary>
public class QuoteComment : BaseEntity
{
    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public int? ParentCommentId { get; set; }
    public QuoteComment? ParentComment { get; set; }
    public ICollection<QuoteComment> Replies { get; set; } = new List<QuoteComment>();
}
