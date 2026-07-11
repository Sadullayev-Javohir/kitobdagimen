using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetMessages;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, PagedResult<MessageDto>>
{
    private const int MaxPageSize = 100;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMessagesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Suhbat", request.ConversationId);

        if (conversation.User1Id != userId && conversation.User2Id != userId)
        {
            throw new ForbiddenAccessException();
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var source = _db.Messages.Where(m => m.ConversationId == request.ConversationId && !m.IsDeleted);
        var totalCount = await source.CountAsync(cancellationToken);

        // Page from newest backwards, then present each page oldest-to-newest for display.
        var items = await source
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToMessageDto(userId, _currentUser.Email?.ToLowerInvariant())
            .ToListAsync(cancellationToken);

        items.Reverse();

        await _db.AttachReactionsAsync(items, userId, cancellationToken);

        return PagedResult<MessageDto>.Create(items, page, pageSize, totalCount);
    }
}
