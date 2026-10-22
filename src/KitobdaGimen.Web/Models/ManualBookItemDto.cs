namespace KitobdaGimen.Web.Models;

public class ManualBookItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string? CoverUrl { get; set; }
    public int TotalPages { get; set; }
    public int PostsCount { get; set; }
    public int QuotesCount { get; set; }
    public int GoalsCount { get; set; }
    public int PhysicalBooksCount { get; set; }
}
