namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Join entity linking a user to a genre they are interested in (selected during onboarding).
/// Uses a composite key (UserId, GenreId) — no surrogate Id.
/// </summary>
public class UserGenre
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
