namespace KitobdaGimen.Application.Features.Auth.Dtos;

/// <summary>Public representation of an authenticated user.</summary>
public record UserDto
{
    public int Id { get; init; }
    public string Email { get; init; } = null!;
    public string? Username { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Authorization role (User / Admin / SuperAdmin).</summary>
    public KitobdaGimen.Domain.Enums.UserRole Role { get; init; }
}
