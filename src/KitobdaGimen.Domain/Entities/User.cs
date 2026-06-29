using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A platform user. Authentication is via Google OAuth only (no password).
/// </summary>
public class User : BaseEntity
{
    public string GoogleId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Username { get; set; }
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Authorization role (User / Admin / SuperAdmin). Defaults to User.</summary>
    public KitobdaGimen.Domain.Enums.UserRole Role { get; set; } = KitobdaGimen.Domain.Enums.UserRole.User;

    /// <summary>Last time the user was seen online. Updated on SignalR disconnect; null if never connected.</summary>
    public DateTime? LastSeenAt { get; set; }

    // Navigation
    public ICollection<UserGenre> UserGenres { get; set; } = new List<UserGenre>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Story> Stories { get; set; } = new List<Story>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<ReadingGoal> ReadingGoals { get; set; } = new List<ReadingGoal>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<SavedQuote> SavedQuotes { get; set; } = new List<SavedQuote>();

    /// <summary>Follow rows where this user is the follower (people this user follows).</summary>
    public ICollection<Follow> Following { get; set; } = new List<Follow>();
    /// <summary>Follow rows where this user is being followed (this user's followers).</summary>
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();

    /// <summary>Chat invites this user has sent (RequesterId).</summary>
    public ICollection<Connection> SentConnections { get; set; } = new List<Connection>();
    /// <summary>Chat invites this user has received (AddresseeId).</summary>
    public ICollection<Connection> ReceivedConnections { get; set; } = new List<Connection>();

    /// <summary>Activity notifications addressed to this user.</summary>
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
