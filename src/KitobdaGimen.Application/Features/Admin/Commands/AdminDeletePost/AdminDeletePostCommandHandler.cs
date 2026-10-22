using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin.Commands.AdminDeletePost;

public class AdminDeletePostCommandHandler : IRequestHandler<AdminDeletePostCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITelegramBotService _telegramBot;

    public AdminDeletePostCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITelegramBotService telegramBot)
    {
        _db = db;
        _currentUser = currentUser;
        _telegramBot = telegramBot;
    }

    public async Task<Unit> Handle(AdminDeletePostCommand request, CancellationToken cancellationToken)
    {
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.Admin, cancellationToken);

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken)
            ?? throw new NotFoundException("Taqriz", request.PostId);

        if (post.TelegramChatId.HasValue && post.TelegramMessageId.HasValue)
        {
            _ = _telegramBot.DeleteMessageAsync(post.TelegramChatId.Value, post.TelegramMessageId.Value, CancellationToken.None);
        }

        // Likes/Comments/Views cascade from the post at the DB.
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
