using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Commands.DeletePost;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITelegramBotService _telegramBot;

    public DeletePostCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITelegramBotService telegramBot)
    {
        _db = db;
        _currentUser = currentUser;
        _telegramBot = telegramBot;
    }

    public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken)
            ?? throw new NotFoundException("Taqriz", request.PostId);

        if (post.UserId != userId)
        {
            throw new ForbiddenAccessException("Faqat o'z taqrizingizni o'chira olasiz.");
        }

        if (post.TelegramChatId.HasValue && post.TelegramMessageId.HasValue)
        {
            _ = _telegramBot.DeleteMessageAsync(post.TelegramChatId.Value, post.TelegramMessageId.Value, CancellationToken.None);
        }

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
