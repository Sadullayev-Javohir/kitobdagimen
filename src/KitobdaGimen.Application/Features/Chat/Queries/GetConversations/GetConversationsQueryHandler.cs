using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetConversations;

public class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetConversationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ConversationDto>> Handle(
        GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var viewerEmail = _currentUser.Email?.ToLowerInvariant();

        // Chat list is driven by accepted connections: the other participant must have an
        // accepted invite with the current user (so people appear only after "qabul qilish").
        var acceptedPartnerIds = await _db.Connections
            .Where(c => c.Status == ConnectionStatus.Accepted
                        && (c.RequesterId == userId || c.AddresseeId == userId))
            .Select(c => c.RequesterId == userId ? c.AddresseeId : c.RequesterId)
            .ToListAsync(cancellationToken);

        var conversations = await _db.Conversations
            .Where(c => (c.User1Id == userId && acceptedPartnerIds.Contains(c.User2Id))
                        || (c.User2Id == userId && acceptedPartnerIds.Contains(c.User1Id)))
            .Select(c => new
            {
                Dto = new ConversationDto
                {
                    Id = c.Id,
                    OtherUser = c.User1Id == userId
                        ? new UserSummaryDto
                        {
                            Id = c.User2.Id,
                            Username = c.User2.Username,
                            FullName = c.User2.FullName,
                            AvatarUrl = (c.User2.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                                         && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                                         && viewerEmail != AvatarPrivacy.RestrictedEmail)
                                ? null
                                : c.User2.AvatarUrl
                        }
                        : new UserSummaryDto
                        {
                            Id = c.User1.Id,
                            Username = c.User1.Username,
                            FullName = c.User1.FullName,
                            AvatarUrl = (c.User1.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                                         && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                                         && viewerEmail != AvatarPrivacy.RestrictedEmail)
                                ? null
                                : c.User1.AvatarUrl
                        },
                    LastSeenAt = c.User1Id == userId ? c.User2.LastSeenAt : c.User1.LastSeenAt,
                    LastMessageText = c.Messages
                        .Where(m => !m.IsDeleted)
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Text ?? "📖 Post ulashildi")
                        .FirstOrDefault(),
                    LastMessageAt = c.Messages
                        .Where(m => !m.IsDeleted)
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => (DateTime?)m.SentAt)
                        .FirstOrDefault(),
                    UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead && !m.IsDeleted)
                },
                SortKey = c.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => (DateTime?)m.SentAt)
                    .FirstOrDefault() ?? c.CreatedAt
            })
            .OrderByDescending(x => x.SortKey)
            .Select(x => x.Dto)
            .ToListAsync(cancellationToken);

        return conversations;
    }
}
