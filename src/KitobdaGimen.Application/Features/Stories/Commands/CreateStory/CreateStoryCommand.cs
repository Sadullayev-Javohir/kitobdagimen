using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Stories.Commands.CreateStory;

/// <summary>Creates a story for the current user from a title and text.</summary>
public record CreateStoryCommand : IRequest<StoryDto>
{
    public string Title { get; init; } = null!;
    public string Text { get; init; } = null!;
    public string? ImageUrl { get; init; }

    /// <summary>How long the story stays visible, in hours. Allowed: 12, 24 or 48.</summary>
    public int DurationHours { get; init; }
}
