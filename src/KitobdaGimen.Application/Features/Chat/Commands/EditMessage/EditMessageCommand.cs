using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Commands.EditMessage;

/// <summary>Edits the text of a message the current user sent.</summary>
public record EditMessageCommand(int MessageId, string Text) : IRequest<MessageDto>;
