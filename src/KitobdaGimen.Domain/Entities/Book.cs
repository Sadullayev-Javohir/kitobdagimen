using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A book that users read, review and quote from.
/// </summary>
public class Book : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string? CoverUrl { get; set; }
    public int TotalPages { get; set; }

    /// <summary>
    /// Kitob ma'lumoti olingan tashqi manba (masalan, "asaxiy.uz"). Foydalanuvchi qo'lda
    /// kiritgan kitoblarda null bo'ladi. Webda manba ko'rsatish (attribution) uchun ishlatiladi.
    /// </summary>
    public string? Source { get; set; }

    public int? GenreId { get; set; }
    public Genre? Genre { get; set; }

    // Navigation
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<ReadingGoal> ReadingGoals { get; set; } = new List<ReadingGoal>();
}
