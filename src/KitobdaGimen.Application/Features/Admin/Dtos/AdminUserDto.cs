using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Application.Features.Admin.Dtos;

/// <summary>A user row shown on the /admin page.</summary>
public record AdminUserDto
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public UserRole Role { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastSeenAt { get; init; }
    public int PostCount { get; init; }
    public int QuoteCount { get; init; }
}
