using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetConversations;

/// <summary>Returns the current user's conversations, most recently active first.</summary>
public record GetConversationsQuery : IRequest<IReadOnlyList<ConversationDto>>;
