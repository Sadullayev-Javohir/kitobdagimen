using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Admin;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetChatMonitoring;

public class GetChatMonitoringQueryHandler
    : IRequestHandler<GetChatMonitoringQuery, ChatMonitoringResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetChatMonitoringQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ChatMonitoringResult> Handle(
        GetChatMonitoringQuery request, CancellationToken cancellationToken)
    {
        // Only a super admin may audit another user's private messages.
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.SuperAdmin, cancellationToken);

        var user = await _db.Users
            .Where(u => u.Id == request.TargetUserId)
            .Select(u => new MonitoredUser
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                AvatarUrl = u.AvatarUrl
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", request.TargetUserId);

        // Distinct conversation partners (one other person per 1:1 conversation).
        var partners = await _db.Conversations
            .Where(c => c.User1Id == request.TargetUserId || c.User2Id == request.TargetUserId)
            .Select(c => c.User1Id == request.TargetUserId ? c.User2Id : c.User1Id)
            .Distinct()
            .CountAsync(cancellationToken);

        var sent = await _db.Messages
            .CountAsync(m => m.SenderId == request.TargetUserId, cancellationToken);
        var edited = await _db.Messages
            .CountAsync(m => m.SenderId == request.TargetUserId && m.EditedAt != null, cancellationToken);
        var deleted = await _db.Messages
            .CountAsync(m => m.SenderId == request.TargetUserId && m.IsDeleted, cancellationToken);

        // Activity by hour of day (UTC). DateTime.Hour translates to date_part on PostgreSQL and
        // works identically on the in-memory test provider.
        var hourRows = await _db.Messages
            .Where(m => m.SenderId == request.TargetUserId)
            .GroupBy(m => m.SentAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var byHour = new int[24];
        foreach (var row in hourRows)
        {
            byHour[row.Hour] = row.Count;
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var messages = await _db.Messages
            .Where(m => m.SenderId == request.TargetUserId)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MonitoredMessage
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                PartnerId = m.Conversation.User1Id == request.TargetUserId
                    ? m.Conversation.User2Id
                    : m.Conversation.User1Id,
                PartnerName = m.Conversation.User1Id == request.TargetUserId
                    ? m.Conversation.User2.FullName
                    : m.Conversation.User1.FullName,
                PartnerAvatarUrl = m.Conversation.User1Id == request.TargetUserId
                    ? m.Conversation.User2.AvatarUrl
                    : m.Conversation.User1.AvatarUrl,
                SentAt = m.SentAt,
                Text = m.Text,
                ImageUrl = m.ImageUrl,
                StickerKey = m.StickerKey,
                VoiceUrl = m.VoiceUrl,
                VoiceDurationSeconds = m.VoiceDurationSeconds,
                IsDeleted = m.IsDeleted,
                DeletedAt = m.DeletedAt,
                IsEdited = m.EditedAt != null,
                EditedAt = m.EditedAt
            })
            .ToListAsync(cancellationToken);

        return new ChatMonitoringResult
        {
            User = user,
            Stats = new ChatMonitoringStats
            {
                ConversationPartners = partners,
                MessagesSent = sent,
                MessagesEdited = edited,
                MessagesDeleted = deleted
            },
            ActivityByHourUtc = byHour,
            Messages = PagedResult<MonitoredMessage>.Create(messages, page, pageSize, sent)
        };
    }
}
