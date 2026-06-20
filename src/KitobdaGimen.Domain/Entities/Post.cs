using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A user's post about a book — a review/impression shown in the feed.
/// </summary>
public class Post : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    /// <summary>
    /// Short random public identifier (12 chars) used in the shareable URL
    /// <c>/post/{username}/{slug}</c> instead of the sequential database id.
    /// </summary>
    public string Slug { get; set; } = null!;

    public string ReviewText { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostView> Views { get; set; } = new List<PostView>();
}
