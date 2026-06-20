namespace KitobdaGimen.Application.Features.Quotes.Dtos;

/// <summary>Result of toggling a saved quote.</summary>
public record SaveQuoteResultDto
{
    public bool IsSaved { get; init; }
    public int SaveCount { get; init; }
}
