using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A one-to-one chat conversation between two users. (User1Id, User2Id) is unique.
/// </summary>
public class Conversation : BaseEntity
{
    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;

    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
