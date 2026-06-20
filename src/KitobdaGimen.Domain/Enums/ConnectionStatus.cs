namespace KitobdaGimen.Domain.Enums;

/// <summary>
/// State of a chat connection (invite) between two users.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>Invite sent, awaiting the addressee's response.</summary>
    Pending = 0,

    /// <summary>Invite accepted — the two users can chat and appear in each other's chat list.</summary>
    Accepted = 1,

    /// <summary>Invite declined by the addressee.</summary>
    Declined = 2
}
