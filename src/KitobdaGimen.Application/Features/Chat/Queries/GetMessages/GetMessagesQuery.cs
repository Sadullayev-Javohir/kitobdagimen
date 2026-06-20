using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetMessages;

/// <summary>Returns messages in a conversation, oldest-to-newest within the page (page 1 = latest).</summary>
public record GetMessagesQuery : IRequest<PagedResult<MessageDto>>
{
    public int ConversationId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 30;
}
