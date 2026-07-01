using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Commands.ToggleReaction;

/// <summary>
/// Toggles the current user's emoji reaction on a message (Telegram-style):
/// adds it, replaces the previous emoji, or removes it when the same emoji is tapped again.
/// </summary>
public record ToggleReactionCommand(int MessageId, string Emoji) : IRequest<MessageDto>;
