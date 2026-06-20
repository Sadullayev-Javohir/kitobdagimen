using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Commands.DeleteMessage;

/// <summary>Deletes a message the current user sent.</summary>
public record DeleteMessageCommand(int MessageId) : IRequest;
