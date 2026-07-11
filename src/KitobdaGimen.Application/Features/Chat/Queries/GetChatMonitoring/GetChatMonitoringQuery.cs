using KitobdaGimen.Application.Common.Models;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetChatMonitoring;

/// <summary>
/// Super-admin audit of everything a given user has sent in chat: who they talked to, how many
/// messages they edited / deleted, when they were active (by hour of day), and a full, reverse
/// chronological message log that includes soft-deleted messages (marked as deleted).
/// </summary>
public class GetChatMonitoringQuery : IRequest<ChatMonitoringResult>
{
    public int TargetUserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class ChatMonitoringResult
{
    public MonitoredUser User { get; init; } = null!;
    public ChatMonitoringStats Stats { get; init; } = null!;
    /// <summary>Message counts bucketed by hour of day (UTC), length 24.</summary>
    public int[] ActivityByHourUtc { get; init; } = Array.Empty<int>();
    public PagedResult<MonitoredMessage> Messages { get; init; } = null!;
}

public class MonitoredUser
{
    public int Id { get; init; }
    public string FullName { get; init; } = "";
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }
}

public class ChatMonitoringStats
{
    /// <summary>Distinct people this user has exchanged messages with.</summary>
    public int ConversationPartners { get; init; }
    /// <summary>Total messages sent (including edited and soft-deleted).</summary>
    public int MessagesSent { get; init; }
    public int MessagesEdited { get; init; }
    public int MessagesDeleted { get; init; }
}

public class MonitoredMessage
{
    public int Id { get; init; }
    public int ConversationId { get; init; }
    public int PartnerId { get; init; }
    public string PartnerName { get; init; } = "";
    public string? PartnerAvatarUrl { get; init; }
    public System.DateTime SentAt { get; init; }
    public string? Text { get; init; }
    public string? ImageUrl { get; init; }
    public string? StickerKey { get; init; }
    public string? VoiceUrl { get; init; }
    public int? VoiceDurationSeconds { get; init; }
    public bool IsDeleted { get; init; }
    public System.DateTime? DeletedAt { get; init; }
    public bool IsEdited { get; init; }
    public System.DateTime? EditedAt { get; init; }
}
