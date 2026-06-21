namespace KitobdaGimen.Application.Common.Models;

/// <summary>Lightweight book reference used inside posts, quotes and reading goals.</summary>
public record BookSummaryDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string Author { get; init; } = null!;
    public string? CoverUrl { get; init; }
    public string? GenreName { get; init; }
    /// <summary>Kitob olingan tashqi manba (masalan "asaxiy.uz"); qo'lda kiritilganda null.</summary>
    public string? Source { get; init; }
}
