using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Commands.GetOrCreateConversation;

/// <summary>Opens (or creates) the one-to-one conversation with another user.</summary>
public record GetOrCreateConversationCommand(int OtherUserId) : IRequest<ConversationDto>;
