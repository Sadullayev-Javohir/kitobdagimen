using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Application.Features.Connections.Dtos;

/// <summary>A chat connection (invite) seen from the current user's perspective.</summary>
public record ConnectionDto
{
    public int Id { get; init; }

    /// <summary>The other participant (not the current user).</summary>
    public UserSummaryDto OtherUser { get; init; } = null!;

    public ConnectionStatus Status { get; init; }

    /// <summary>True when the current user sent the invite; false when they received it.</summary>
    public bool IamRequester { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
}
