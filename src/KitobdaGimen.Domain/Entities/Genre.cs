using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A book genre (e.g. Roman, Ilmiy, Tarix). Used for onboarding interests and book classification.
/// </summary>
public class Genre : BaseEntity
{
    public string Name { get; set; } = null!;

    // Navigation
    public ICollection<UserGenre> UserGenres { get; set; } = new List<UserGenre>();
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
