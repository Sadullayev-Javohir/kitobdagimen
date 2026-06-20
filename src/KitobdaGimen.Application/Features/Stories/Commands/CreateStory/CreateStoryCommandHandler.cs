using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Stories.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Stories.Commands.CreateStory;

public class CreateStoryCommandHandler : IRequestHandler<CreateStoryCommand, StoryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateStoryCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StoryDto> Handle(CreateStoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var now = DateTime.UtcNow;
        var story = new Story
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Text = request.Text.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl,
            CreatedAt = now,
            ExpiresAt = now.AddHours(request.DurationHours)
        };

        _db.Stories.Add(story);
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Stories
            .Where(s => s.Id == story.Id)
            .ToStoryDto(userId)
            .FirstAsync(cancellationToken);
    }
}
