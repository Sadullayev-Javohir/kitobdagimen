namespace KitobdaGimen.Application.Common.Models;

/// <summary>Lightweight user reference used inside posts, comments, messages, etc.</summary>
public record UserSummaryDto
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
}
