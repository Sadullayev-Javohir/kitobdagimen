using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.Quotes.Dtos;

/// <summary>A book quote with its author, source book and save state.</summary>
public record QuoteDto
{
    public int Id { get; init; }
    public string Text { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public UserSummaryDto Author { get; init; } = null!;
    public BookSummaryDto Book { get; init; } = null!;
    public int SaveCount { get; init; }
    public bool IsSavedByCurrentUser { get; init; }
}
