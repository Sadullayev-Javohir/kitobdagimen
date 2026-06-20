using KitobdaGimen.Domain.Common;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A chat connection (invite) between two users. Before two users can chat, one must send an
/// invite (<see cref="ConnectionStatus.Pending"/>) and the other must accept it
/// (<see cref="ConnectionStatus.Accepted"/>). (RequesterId, AddresseeId) is unique.
/// </summary>
public class Connection : BaseEntity
{
    /// <summary>The user who sent the invite.</summary>
    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    /// <summary>The user who received the invite.</summary>
    public int AddresseeId { get; set; }
    public User Addressee { get; set; } = null!;

    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;

    public DateTime CreatedAt { get; set; }

    /// <summary>When the addressee accepted or declined the invite (null while pending).</summary>
    public DateTime? RespondedAt { get; set; }
}
