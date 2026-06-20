using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Commands.SendMessage;

/// <summary>
/// Sends a message. Provide either an existing <see cref="ConversationId"/> or a
/// <see cref="RecipientId"/> (a conversation is created on demand). At least one of
/// <see cref="Text"/> or <see cref="SharedPostId"/> must be supplied.
/// </summary>
public record SendMessageCommand : IRequest<MessageDto>
{
    public int? ConversationId { get; init; }
    public int? RecipientId { get; init; }
    public string? Text { get; init; }
    public int? SharedPostId { get; init; }
}
