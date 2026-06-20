using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A user's story: a short title and text. Displayed as a tappable ring on the author's avatar in
/// the feed/profile, opening a full-screen viewer.
/// </summary>
public class Story : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Story heading shown at the top of the viewer.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Story body text.</summary>
    public string Text { get; set; } = null!;

    /// <summary>Optional uploaded image shown above the text.</summary>
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the story stops being shown. After this moment it is treated as expired everywhere
    /// (no ring on the avatar, hidden from the viewer) so the profile returns to its plain look.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    // Navigation
    public ICollection<StoryView> Views { get; set; } = new List<StoryView>();
    public ICollection<StoryLike> Likes { get; set; } = new List<StoryLike>();
}
