using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat.Commands.GetOrCreateConversation;

public class GetOrCreateConversationCommandHandler
    : IRequestHandler<GetOrCreateConversationCommand, ConversationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrCreateConversationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ConversationDto> Handle(
        GetOrCreateConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        if (request.OtherUserId == userId)
        {
            throw new ForbiddenAccessException("O'zingiz bilan suhbatlasha olmaysiz.");
        }

        var other = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.OtherUserId, cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", request.OtherUserId);

        var conversation = await ConversationHelper.GetOrCreateAsync(
            _db, userId, request.OtherUserId, cancellationToken);

        return new ConversationDto
        {
            Id = conversation.Id,
            OtherUser = new UserSummaryDto
            {
                Id = other.Id,
                FullName = other.FullName,
                AvatarUrl = other.AvatarUrl
            },
            LastMessageText = null,
            LastMessageAt = null,
            UnreadCount = 0
        };
    }
}
