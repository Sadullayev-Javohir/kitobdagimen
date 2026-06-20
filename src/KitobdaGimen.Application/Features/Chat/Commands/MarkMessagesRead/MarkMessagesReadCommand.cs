using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Commands.MarkMessagesRead;

/// <summary>Marks all messages from the other participant in a conversation as read.</summary>
public record MarkMessagesReadCommand(int ConversationId) : IRequest<Unit>;
